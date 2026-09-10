using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotEvaluator
    {
        [Inject] private readonly BotForecast _forecast;
        [Inject] private readonly BotProfileProvider _profiles;

        public float Score(GameContext context, Player me, BotMove move, out BotOutcome outcome)
        {
            var profile = _profiles.Default;
            var weights = profile.Weights;

            outcome = _forecast.Predict(context, me, move);

            var opponent = context.GetOpponent(me);
            var definition = move.Card.Definition;
            var boardOwner = CardPlayService.IsTraitor(move.Card) ? opponent : me;

            var score = outcome.EnemyPowerLost * weights.EnemyDamage
                        - outcome.AllyPowerLost * weights.AllyDamage
                        - outcome.AllyKills * weights.AllyKill
                        + outcome.SelfPowerGain
                        + outcome.SummonedPower;

            var body = BotCardTraits.PowerOf(definition) * weights.BodyPower;
            score += boardOwner == me ? body : -body;

            if (outcome.AllyPowerLost > outcome.EnemyPowerLost)
                score -= weights.BadExchange;

            var killReference = weights.KillValuePower > 0f ? weights.KillValuePower : 1f;

            score += outcome.WishTheirLoss - outcome.WishMyLoss + outcome.WishMyGain - outcome.WishTheirGain;

            if (outcome.WishMyGain > 0) score += weights.HatchGain;

            if (outcome.WishMyLoss > 0 || outcome.WishTheirGain > 0)
                score -= weights.DeathwishPenalty;

            foreach (var victim in outcome.EnemyVictims)
            {
                var worth = victim.CurrentPower.Value / killReference;
                var kill = weights.EnemyKill * (worth < 1f ? worth : 1f);

                if (BotCardTraits.HasBond(victim.Definition))
                    score += weights.BondKill;

                if (outcome.Wishes.TryGetValue(victim, out var wish)
                    && wish.TheirLoss == 0
                    && wish.MyLoss == 0)
                {
                    kill *= weights.IdleDeathwishKill;
                }

                score += kill;
            }

            var comboReady = boardOwner != me
                             && HasFinisher(me, move.Card)
                             && OccupiedNeighbors(boardOwner, move) > 0;

            if (comboReady)
            {
                score += weights.TraitorCombo;
                score += SnipeScore(me, boardOwner, move, weights);
            }

            if (outcome.NeedsAim && outcome.AimPool == 0)
                score -= weights.WastedAim;

            if (!comboReady && outcome.Swing < 0 && !outcome.AppliesWeather && !outcome.NeedsAim)
                score -= weights.NoGain;

            score += WeatherScore(context, me, opponent, move, outcome, profile);
            score += PreparationScore(context, me, opponent, move, profile);
            score += PlacementScore(me, boardOwner, move, definition, profile);

            if (definition.Id == profile.SweeperCardId)
            {
                if (outcome.EnemyKills - outcome.AllyKills < profile.SweeperMinKills)
                    score -= weights.SweeperHold;
            }
            else if (Holds(me, profile.SweeperCardId))
            {
                score += outcome.EnemiesAtOne * weights.LeshyPrep;
            }

            return score;
        }

        private static float WeatherScore(GameContext context, Player me, Player opponent, BotMove move,
            BotOutcome outcome, BotProfile profile)
        {
            if (!outcome.AppliesWeather) return 0f;

            var weights = profile.Weights;
            var target = move.EnemyRow ? opponent : me;

            if (target.GetWeather(move.Row)?.CardId == move.Card.Definition.Id)
                return weights.WeatherOnOwnRow;

            if (!move.EnemyRow) return weights.WeatherOnOwnRow;

            return move.Row == RowType.Melee ? weights.WeatherOnEnemyMelee : weights.WeatherOnEnemyRanged;
        }

        private static float PreparationScore(GameContext context, Player me, Player opponent, BotMove move,
            BotProfile profile)
        {
            var weights = profile.Weights;
            var score = 0f;

            var weatherOnFront = opponent.GetWeather(RowType.Melee)?.CardId == profile.WeatherCardId;

            if (!weatherOnFront
                && move.Card is UnitInstance
                && Holds(me, profile.WeatherCasterId)
                && !Holds(me, profile.WeatherCardId)
                && me.MeleeRow.Count + me.RangedRow.Count == 0)
            {
                score += weights.VodyanoyPrep - BotCardTraits.PowerOf(move.Card.Definition);
            }

            if (!weatherOnFront
                && move.Card.Definition.Id == profile.WeatherCasterId
                && me.MeleeRow.Count + me.RangedRow.Count > 0)
            {
                score += weights.VodyanoyPrep;
            }

            if (weatherOnFront && BotCardTraits.Moves(move.Card.Definition) && opponent.RangedRow.Count > 0)
                score += weights.PullUnderWeather;

            return score;
        }

        private static float PlacementScore(Player me, Player boardOwner, BotMove move,
            CardDefinition definition, BotProfile profile)
        {
            if (move.Card is not UnitInstance) return 0f;

            var weights = profile.Weights;
            var score = 0f;

            if (BotCardTraits.NeedsMelee(definition))
                score += move.Row == RowType.Melee ? weights.BackRow : -weights.BackRow * 4f;
            else if (BotCardTraits.IsPassive(definition))
                score += move.Row == RowType.Ranged ? weights.BackRow : -weights.FrontRowPassive;

            if (boardOwner == me
                && BotForecast.WeatherBite(me, move.Row) > 0
                && !BotCardTraits.RequiresRow(definition, move.Row))
            {
                score -= weights.WeatherRow;
            }

            if (!Match.BoardConfig.IsValidSlot(move.Slot)) return score;

            var slot = boardOwner.GetRow(move.Row).Slots[move.Slot];

            if (boardOwner != me)
                return score + TraitorScore(slot, weights);

            if (BotCardTraits.SummonsToNeighbors(definition))
            {
                var room = FreeNeighbors(slot);

                score += room > 0 ? weights.SummonRoom * room : -weights.NoSummonRoom;
            }

            if (move.Row == RowType.Ranged && !me.GetRow(RowType.Melee).Slots[move.Slot].IsEmpty)
                score += weights.Shield;

            if (NextToTraitor(slot)) score -= weights.TraitorAdjacency;

            return score;
        }

        private static bool NextToTraitor(BoardSlot slot)
        {
            return Traitor(slot.Left) || Traitor(slot.Right);
        }

        private static bool Traitor(BoardSlot slot)
        {
            return slot?.Unit != null && slot.Unit.UnitDefinition.Traitor;
        }

        private static float TraitorScore(BoardSlot slot, BotWeights weights)
        {
            var score = 0f;

            score += NeighborValue(slot.Left, weights);
            score += NeighborValue(slot.Right, weights);

            return score;
        }

        private static float NeighborValue(BoardSlot neighbor, BotWeights weights)
        {
            if (neighbor?.Unit == null) return 0f;

            var score = weights.TraitorNeighbor;

            if (BotCardTraits.DevoursNeighbors(neighbor.Unit.Definition))
                score += weights.TraitorDevourer;

            return score;
        }

        private static int FreeNeighbors(BoardSlot slot)
        {
            var count = 0;

            if (slot.Left is { IsEmpty: true }) count++;
            if (slot.Right is { IsEmpty: true }) count++;

            return count;
        }

        private static bool HasFinisher(Player me, CardInstance traitor)
        {
            var power = BotCardTraits.PowerOf(traitor.Definition);

            foreach (var card in me.Hand)
            {
                if (card == traitor) continue;

                if (BotCardTraits.DirectDamage(card.Definition) >= power) return true;
            }

            return false;
        }

        private static float SnipeScore(Player me, Player boardOwner, BotMove move, BotWeights weights)
        {
            if (!Match.BoardConfig.IsValidSlot(move.Slot)) return 0f;

            var blast = BotCardTraits.DeathwishDamage(move.Card.Definition);
            if (blast <= 0) return 0f;

            var slot = boardOwner.GetRow(move.Row).Slots[move.Slot];

            return NeighborSnipe(me, slot.Left, blast, weights)
                   + NeighborSnipe(me, slot.Right, blast, weights);
        }

        private static float NeighborSnipe(Player me, BoardSlot neighbor, int blast, BotWeights weights)
        {
            var unit = neighbor?.Unit;
            if (unit == null) return 0f;

            if (unit.CurrentPower.Value + unit.Armor.Value > blast) return 0f;

            var score = weights.TraitorKill;

            if (BotCardTraits.IsPassive(unit.Definition) && !CanKillDirectly(me, unit))
                score += weights.TraitorSnipe;

            return score;
        }

        private static bool CanKillDirectly(Player me, UnitInstance target)
        {
            var health = target.CurrentPower.Value + target.Armor.Value;

            foreach (var card in me.Hand)
            {
                if (!BotCardTraits.AimsManually(card.Definition)) continue;
                if (BotCardTraits.DirectDamage(card.Definition) >= health) return true;
            }

            return false;
        }

        private static int OccupiedNeighbors(Player boardOwner, BotMove move)
        {
            if (!Match.BoardConfig.IsValidSlot(move.Slot)) return 0;

            var slot = boardOwner.GetRow(move.Row).Slots[move.Slot];
            var count = 0;

            if (slot.Left?.Unit != null) count++;
            if (slot.Right?.Unit != null) count++;

            return count;
        }

        public static bool Holds(Player player, string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return false;

            foreach (var card in player.Hand)
                if (card.Definition.Id == cardId)
                    return true;

            return false;
        }
    }
}
