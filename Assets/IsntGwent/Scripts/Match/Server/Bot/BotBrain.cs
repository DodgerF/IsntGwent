using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Server.Effects;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotBrain : IBotBrain
    {
        [Inject] private readonly BotMoveEnumerator _moves;
        [Inject] private readonly BotEvaluator _evaluator;
        [Inject] private readonly BotForecast _forecast;
        [Inject] private readonly BotRoundPolicy _round;
        [Inject] private readonly BotRedrawPolicy _redraw;
        [Inject] private readonly BotProfileProvider _profiles;

        public BotMove Decide(GameContext context, Player me)
        {
            var moves = _moves.Enumerate(context, me);

            BotMove safeBest = null;
            BotMove harmfulBest = null;
            BotMove losingBest = null;

            var safeScore = float.NegativeInfinity;
            var harmfulScore = float.NegativeInfinity;
            var losingScore = float.NegativeInfinity;

            var safeSwing = int.MinValue;
            var harmfulSwing = int.MinValue;

            foreach (var move in moves)
            {
                var score = _evaluator.Score(context, me, move, out var outcome);
                var swing = Swing(context, me, move, outcome);

                if (swing < 0)
                {
                    if (score <= losingScore) continue;

                    losingScore = score;
                    losingBest = move;
                    continue;
                }

                if (HarmsAllies(outcome))
                {
                    if (swing > harmfulSwing) harmfulSwing = swing;
                    if (score <= harmfulScore) continue;

                    harmfulScore = score;
                    harmfulBest = move;
                    continue;
                }

                if (swing > safeSwing) safeSwing = swing;
                if (score <= safeScore) continue;

                safeScore = score;
                safeBest = move;
            }

            if (me.PendingPlays.Count > 0)
                return safeBest ?? harmfulBest ?? losingBest ?? BotMove.Pass();

            var best = safeBest ?? harmfulBest;
            if (best == null) return BotMove.Pass();

            var bestScore = safeBest != null ? safeScore : harmfulScore;
            var maxSwing = safeBest != null ? safeSwing : harmfulSwing;

            if (maxSwing == int.MinValue) maxSwing = 0;

            var strategic = bestScore >= _profiles.Default.StrategicScore;

            if (_round.ShouldPass(context, me, true, maxSwing, strategic))
                return BotMove.Pass();

            return best;
        }

        private static bool HarmsAllies(BotOutcome outcome)
        {
            var mine = outcome.AllyPowerLost + outcome.WishMyLoss - outcome.WishMyGain;
            var theirs = outcome.EnemyPowerLost + outcome.WishTheirLoss;

            return mine > theirs;
        }

        public List<string> ChooseAim(GameContext context, Player me, IReadOnlyList<string> pool)
        {
            var result = new List<string>();
            if (pool == null || pool.Count == 0) return result;

            var candidates = new List<UnitInstance>();

            foreach (var id in pool)
            {
                var unit = Find(context, id);
                if (unit != null) candidates.Add(unit);
            }

            if (candidates.Count == 0) return result;

            var pending = context.PendingAim;
            var definition = pending?.Card.Definition;
            var limit = Limit(pending);

            candidates.Sort((left, right) => Value(context, me, definition, right)
                .CompareTo(Value(context, me, definition, left)));

            for (var i = 0; i < candidates.Count && i < limit; i++)
                result.Add(candidates[i].Id.ToString());

            return result;
        }

        public string ChooseRedraw(GameContext context, Player me) => _redraw.Choose(context, me);

        private static int Limit(PendingAim pending)
        {
            if (pending?.Card == null) return 1;

            var effects = pending.Card.Definition.Effects;
            if (pending.EffectIndex < 0 || pending.EffectIndex >= effects.Count) return 1;

            return effects[pending.EffectIndex] is ManualTargetingDefinition { Count: > 0 } manual
                ? manual.Count
                : 1;
        }

        private float Value(GameContext context, Player me, CardDefinition definition, UnitInstance unit)
        {
            var profile = _profiles.Default;
            var isAlly = me.FindSlot(unit) != null;
            var wish = Wish(context, me, unit);

            if (isAlly)
            {
                float cost = -unit.CurrentPower.Value;

                if (BotCardTraits.IsPassive(unit.Definition)) cost -= 3f;

                cost += WishValue(wish);

                if (wish.MyGain > 0 && definition != null && BotCardTraits.Devours(definition))
                    cost += profile.Weights.HatchGain;

                return cost;
            }

            float value = unit.CurrentPower.Value;

            if (definition != null && BotCardTraits.Moves(definition))
                value += MoveValue(context, me, definition, unit, profile);

            value += WishValue(wish);

            if (BotCardTraits.HasBond(unit.Definition)) value += profile.Weights.BondKill;

            return value;
        }

        private static float MoveValue(GameContext context, Player me, CardDefinition definition,
            UnitInstance unit, BotProfile profile)
        {
            var weights = profile.Weights;
            var owner = me.FindSlot(unit) != null ? me : context.GetOpponent(me);
            var from = owner.FindSlot(unit);

            if (from == null) return 0f;

            var destination = BoardGeometry.OtherRow(from);

            if (destination == null || !destination.IsEmpty) return -weights.WastedAim;

            var value = 0f;
            var bite = BotForecast.WeatherBite(owner, destination.Row);

            if (bite > 0)
            {
                value += weights.MoveIntoWeather;

                if (unit.CurrentPower.Value <= bite) value += weights.EnemyKill;
            }

            if (owner != me)
            {
                if (IsTraitor(destination.Left)) value += weights.TraitorBait;
                if (IsTraitor(destination.Right)) value += weights.TraitorBait;
            }

            var partner = BotCardTraits.BondPartner(definition);

            if (!string.IsNullOrEmpty(partner) && context.HasOnBoard(me, partner))
                value += weights.RowSweep * owner.GetRow(unit.RowType).Count;

            return value;
        }

        private static bool IsTraitor(BoardSlot slot)
        {
            return slot?.Unit != null && slot.Unit.UnitDefinition.Traitor;
        }

        private BotDeathwish Wish(GameContext context, Player me, UnitInstance unit)
        {
            return BotCardTraits.HasDeathwish(unit.Definition)
                ? _forecast.Deathwish(context, me, unit)
                : new BotDeathwish();
        }

        private static float WishValue(BotDeathwish wish)
        {
            return wish.TheirLoss - wish.MyLoss + wish.MyGain - wish.TheirGain;
        }

        private static int Swing(GameContext context, Player me, BotMove move, BotOutcome outcome)
        {
            var boardOwner = CardPlayService.IsTraitor(move.Card) ? context.GetOpponent(me) : me;
            var body = BotCardTraits.PowerOf(move.Card.Definition);

            if (boardOwner != me) body = -body;

            return body + outcome.EnemyPowerLost - outcome.AllyPowerLost
                   + outcome.SelfPowerGain + outcome.SummonedPower + outcome.WishMyGain;
        }

        private static UnitInstance Find(GameContext context, string id)
        {
            foreach (var unit in context.Player1.MeleeRow) if (unit.Id.ToString() == id) return unit;
            foreach (var unit in context.Player1.RangedRow) if (unit.Id.ToString() == id) return unit;
            foreach (var unit in context.Player2.MeleeRow) if (unit.Id.ToString() == id) return unit;
            foreach (var unit in context.Player2.RangedRow) if (unit.Id.ToString() == id) return unit;

            return null;
        }
    }
}
