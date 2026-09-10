using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Cards.Server.Effects;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotForecast
    {
        [Inject] private readonly EffectRegistry _effects;
        [Inject] private readonly Cards.CardDatabase _cards;

        public BotOutcome Predict(GameContext context, Player me, BotMove move)
        {
            var outcome = new BotOutcome();
            if (context == null || me == null || move?.Card == null) return outcome;

            var card = move.Card;
            var boardOwner = CardPlayService.IsTraitor(card) ? context.GetOpponent(me) : me;

            var effectContext = new EffectContext
            {
                Game = context,
                Source = card,
                Owner = boardOwner,
                PlayedRow = move.Row,
                PlayedSlot = move.Slot,
                TargetsEnemyRow = move.EnemyRow,
            };

            var damage = new Dictionary<UnitInstance, int>();

            var unit = card as UnitInstance;
            var savedRow = unit?.RowType ?? RowType.None;

            if (unit != null) unit.RowType = move.Row;

            try
            {
                Run(card, effectContext, EffectTrigger.OnPlay, move.Row, damage, outcome, me);
            }
            finally
            {
                if (unit != null) unit.RowType = savedRow;
            }

            outcome.BodyPower = BotCardTraits.PowerOf(card.Definition);

            if (unit != null)
            {
                var bite = WeatherBite(boardOwner, move.Row);

                if (bite > 0)
                {
                    outcome.WeatherBite = bite < outcome.BodyPower ? bite : outcome.BodyPower;
                    outcome.BodyPower -= outcome.WeatherBite;
                }
            }

            if (boardOwner != me) outcome.BodyPower = -outcome.BodyPower;

            Collect(context, me, damage, outcome);
            CollectWishes(context, me, outcome);

            if (unit != null && boardOwner != me)
                outcome.PlannedGain = PlannedDeathwish(me, boardOwner, card, move);

            return outcome;
        }

        private static int PlannedDeathwish(Player me, Player boardOwner, CardInstance card, BotMove move)
        {
            if (!BotCardTraits.HasDeathwish(card.Definition)) return 0;
            if (!Match.BoardConfig.IsValidSlot(move.Slot)) return 0;
            if (!HasFinisher(me, card)) return 0;

            var blast = BotCardTraits.DeathwishDamage(card.Definition);
            if (blast <= 0) return 0;

            var slot = boardOwner.GetRow(move.Row).Slots[move.Slot];

            return Bitten(slot.Left, blast) + Bitten(slot.Right, blast);
        }

        private static int Bitten(BoardSlot slot, int amount)
        {
            var unit = slot?.Unit;
            if (unit == null) return 0;

            var taken = Bite(unit, amount);

            return taken < unit.CurrentPower.Value ? taken : unit.CurrentPower.Value;
        }

        private static bool HasFinisher(Player me, CardInstance target)
        {
            var power = BotCardTraits.PowerOf(target.Definition);

            foreach (var card in me.Hand)
            {
                if (card == target) continue;
                if (BotCardTraits.DirectDamage(card.Definition) >= power) return true;
            }

            return false;
        }

        private void CollectWishes(GameContext context, Player me, BotOutcome outcome)
        {
            foreach (var victim in outcome.EnemyVictims)
                AddWish(context, me, victim, outcome);

            foreach (var casualty in outcome.AllyCasualties)
                AddWish(context, me, casualty, outcome);
        }

        private void AddWish(GameContext context, Player me, UnitInstance victim, BotOutcome outcome)
        {
            if (!BotCardTraits.HasDeathwish(victim.Definition)) return;

            var wish = Deathwish(context, me, victim);

            outcome.Wishes[victim] = wish;
            outcome.WishMyLoss += wish.MyLoss;
            outcome.WishMyGain += wish.MyGain;
            outcome.WishTheirLoss += wish.TheirLoss;
            outcome.WishTheirGain += wish.TheirGain;
        }

        public static int WeatherBite(Player owner, RowType row)
        {
            var weather = owner?.GetWeather(row);
            if (weather?.Source == null) return 0;

            var total = 0;

            foreach (var effect in weather.Source.Definition.Effects)
            {
                if (effect.Trigger != EffectTrigger.OnWeatherEnter) continue;
                if (effect is not DealDamageEffectDefinition damage || damage.Self) continue;

                total += damage.Amount;
            }

            return total;
        }

        public BotDeathwish Deathwish(GameContext context, Player me, UnitInstance victim)
        {
            var result = new BotDeathwish();
            if (context == null || victim == null) return result;

            var owner = context.Player1.FindSlot(victim) != null ? context.Player1 : context.Player2;
            var slot = owner.FindSlot(victim);

            var effectContext = new EffectContext
            {
                Game = context,
                Source = victim,
                Owner = owner,
                PlayedRow = victim.RowType,
                PlayedSlot = slot?.Index ?? -1,
            };

            var damage = new Dictionary<UnitInstance, int>();
            var outcome = new BotOutcome();

            Run(victim, effectContext, EffectTrigger.OnDeath, victim.RowType, damage, outcome, me);

            foreach (var pair in damage)
            {
                if (pair.Key == victim) continue;

                var taken = System.Math.Min(pair.Key.CurrentPower.Value, Bite(pair.Key, pair.Value));

                if (me.FindSlot(pair.Key) != null)
                    result.MyLoss += taken;
                else
                    result.TheirLoss += taken;
            }

            var gain = outcome.SummonedPower + outcome.SelfPowerGain;

            if (owner == me)
                result.MyGain = gain;
            else
                result.TheirGain = gain;

            return result;
        }

        private void Run(CardInstance card, EffectContext effectContext, EffectTrigger trigger, RowType row,
            Dictionary<UnitInstance, int> damage, BotOutcome outcome, Player me)
        {
            foreach (var definition in card.Definition.Effects)
            {
                if (definition.Trigger != trigger) continue;
                if (definition.RequiredRow != RowType.None && definition.RequiredRow != row) continue;

                effectContext.Definition = definition;

                if (!SlotConditions.IsMet(effectContext, definition)) continue;
                if (!EffectConditions.IsMet(effectContext, definition)) continue;

                var effect = _effects.Get(definition);
                if (!effect.CanTrigger(effectContext)) continue;

                if (definition is AimedTargetingDefinition { Count: > 0 })
                {
                    outcome.NeedsAim = true;
                    outcome.AimPool += ((ManualTargetingEffect)effect).GetPool(effectContext).Count;
                    effectContext.Targets = new List<UnitInstance>();
                    continue;
                }

                if (effect is PowerTargetingEffect power && definition is PowerTargetingDefinition powerDefinition)
                {
                    effectContext.Targets = Pessimistic(power.GetPool(effectContext), powerDefinition, me);
                    continue;
                }

                if (effect is TargetingEffect targeting)
                {
                    effectContext.Targets = targeting.ResolveTargets(effectContext);
                    continue;
                }

                Simulate(effectContext, definition, damage, outcome);
            }
        }

        private static List<UnitInstance> Pessimistic(List<UnitInstance> pool, PowerTargetingDefinition definition,
            Player me)
        {
            var result = new List<UnitInstance>();
            if (pool.Count == 0) return result;

            var highest = definition is StrongestTargetingDefinition;
            var edge = pool[0].CurrentPower.Value;

            foreach (var unit in pool)
            {
                var power = unit.CurrentPower.Value;

                if (highest ? power > edge : power < edge) edge = power;
            }

            var candidates = new List<UnitInstance>();

            foreach (var unit in pool)
                if (unit.CurrentPower.Value == edge)
                    candidates.Add(unit);

            var count = definition.Count;

            if (count <= 0 || count >= candidates.Count) return candidates;

            candidates.Sort((left, right) => Rank(me, left).CompareTo(Rank(me, right)));

            for (var i = 0; i < count; i++)
                result.Add(candidates[i]);

            return result;
        }

        private static int Rank(Player me, UnitInstance unit) => me != null && me.FindSlot(unit) != null ? 0 : 1;

        private void Simulate(EffectContext effectContext,
            EffectDefinition definition, Dictionary<UnitInstance, int> damage, BotOutcome outcome)
        {
            switch (definition)
            {
                case DealDamageEffectDefinition damageDefinition:
                    if (damageDefinition.Self)
                    {
                        outcome.SelfPowerGain -= damageDefinition.Amount;
                        break;
                    }

                    foreach (var target in effectContext.Targets)
                        Add(damage, target, damageDefinition.Amount);

                    break;

                case DestroyEffectDefinition:
                    foreach (var target in effectContext.Targets)
                        Add(damage, target, Effective(target));

                    break;

                case DevourDefinition:
                    foreach (var target in effectContext.Targets)
                    {
                        Add(damage, target, Effective(target));
                        outcome.SelfPowerGain += target.CurrentPower.Value;
                    }

                    break;

                case BuffPowerDefinition buff:
                    if (buff.Self)
                        outcome.SelfPowerGain += buff.Amount;
                    else
                        outcome.SummonedPower += buff.Amount * effectContext.Targets.Count;

                    break;

                case ApplyWeatherDefinition:
                    outcome.AppliesWeather = true;
                    break;

                case MoveEffectDefinition:
                    outcome.MovesUnits = true;
                    break;

                case SummonTokenDefinition token:
                    outcome.SummonedPower += PowerOf(token.TokenId) * (token.Count > 0 ? token.Count : 1);
                    break;

                case CastFromDeckDefinition cast:
                    outcome.SummonedPower += PowerOf(cast.CardId);
                    break;
            }
        }

        private void Collect(GameContext context, Player me, Dictionary<UnitInstance, int> damage,
            BotOutcome outcome)
        {
            var opponent = context.GetOpponent(me);

            foreach (var enemy in Units(opponent))
            {
                damage.TryGetValue(enemy, out var dealt);

                var left = enemy.CurrentPower.Value - Bite(enemy, dealt);

                if (left <= 0)
                {
                    outcome.EnemyPowerLost += enemy.CurrentPower.Value;
                    outcome.EnemyVictims.Add(enemy);
                    continue;
                }

                outcome.EnemyPowerLost += enemy.CurrentPower.Value - left;

                if (left == 1)
                    outcome.EnemiesAtOne++;
            }

            foreach (var ally in Units(me))
            {
                damage.TryGetValue(ally, out var dealt);

                var left = ally.CurrentPower.Value - Bite(ally, dealt);

                if (left <= 0)
                {
                    outcome.AllyPowerLost += ally.CurrentPower.Value;
                    outcome.AllyCasualties.Add(ally);
                    continue;
                }

                outcome.AllyPowerLost += ally.CurrentPower.Value - left;
            }
        }

        private static IEnumerable<UnitInstance> Units(Player player)
        {
            foreach (var unit in player.MeleeRow) yield return unit;
            foreach (var unit in player.RangedRow) yield return unit;
        }

        private static void Add(Dictionary<UnitInstance, int> damage, UnitInstance target, int amount)
        {
            if (target == null || amount <= 0) return;

            damage.TryGetValue(target, out var current);
            damage[target] = current + amount;
        }

        private static int Bite(UnitInstance unit, int dealt)
        {
            if (dealt <= 0) return 0;

            var afterArmor = dealt - unit.Armor.Value;

            return afterArmor > 0 ? afterArmor : 0;
        }

        private static int Effective(UnitInstance unit) => unit.CurrentPower.Value + unit.Armor.Value;

        private int PowerOf(string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return 0;
            if (!_cards.Cards.TryGetValue(cardId, out var definition)) return 0;

            return definition is UnitDefinition unit ? unit.Power : 0;
        }
    }
}
