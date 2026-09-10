using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public static class BotCardTraits
    {
        private static readonly HashSet<EffectTrigger> PassiveTriggers = new()
        {
            EffectTrigger.OnTurnStart,
            EffectTrigger.OnEnemyTurnStart,
            EffectTrigger.OnTurnEnd,
            EffectTrigger.OnCardPlayed,
            EffectTrigger.OnEnemyCardPlayed,
            EffectTrigger.OnUnitMoved,
            EffectTrigger.OnAllyDied,
            EffectTrigger.OnAllySummoned,
            EffectTrigger.OnAllyDevoured,
            EffectTrigger.OnRoundEnd,
        };

        public static bool HasBond(CardDefinition definition)
        {
            foreach (var effect in definition.Effects)
                if (effect.Condition != null && !string.IsNullOrEmpty(effect.Condition.AllyOnBoard))
                    return true;

            return false;
        }

        public static string BondPartner(CardDefinition definition)
        {
            foreach (var effect in definition.Effects)
                if (effect.Condition != null && !string.IsNullOrEmpty(effect.Condition.AllyOnBoard))
                    return effect.Condition.AllyOnBoard;

            return null;
        }

        public static bool HasDeathwish(CardDefinition definition)
        {
            foreach (var effect in definition.Effects)
                if (effect.Trigger == EffectTrigger.OnDeath)
                    return true;

            return false;
        }

        public static bool IsPassive(CardDefinition definition)
        {
            foreach (var effect in definition.Effects)
                if (PassiveTriggers.Contains(effect.Trigger))
                    return true;

            return false;
        }

        public static bool RequiresRow(CardDefinition definition, RowType row)
        {
            if (row == RowType.None) return false;

            if (definition is UnitDefinition unit && unit.RequiredRow == row) return true;

            foreach (var effect in definition.Effects)
                if (effect.RequiredRow == row)
                    return true;

            return false;
        }

        public static bool NeedsMelee(CardDefinition definition)
        {
            if (definition is UnitDefinition unit && unit.RequiredRow == RowType.Melee)
                return true;

            foreach (var effect in definition.Effects)
                if (effect.RequiredRow == RowType.Melee)
                    return true;

            return false;
        }

        public static bool SummonsToNeighbors(CardDefinition definition)
        {
            foreach (var effect in definition.Effects)
                if (effect is SummonTokenDefinition { Placement: SummonPlacement.Neighbors })
                    return true;

            return false;
        }

        public static bool DevoursNeighbors(CardDefinition definition)
        {
            foreach (var effect in definition.Effects)
                if (effect is DevourDefinition && effect.Trigger != EffectTrigger.OnPlay)
                    return true;

            return false;
        }

        public static bool Devours(CardDefinition definition)
        {
            foreach (var effect in definition.Effects)
                if (effect is DevourDefinition)
                    return true;

            return false;
        }

        public static bool Moves(CardDefinition definition)
        {
            foreach (var effect in definition.Effects)
                if (effect is MoveEffectDefinition)
                    return true;

            return false;
        }

        public static bool AimsManually(CardDefinition definition)
        {
            foreach (var effect in definition.Effects)
                if (effect is AimedTargetingDefinition { Count: > 0 })
                    return true;

            return false;
        }

        public static int DeathwishDamage(CardDefinition definition)
        {
            var total = 0;

            foreach (var effect in definition.Effects)
            {
                if (effect.Trigger != EffectTrigger.OnDeath) continue;
                if (effect is not DealDamageEffectDefinition damage || damage.Self) continue;

                total += damage.Amount;
            }

            return total;
        }

        public static int DirectDamage(CardDefinition definition)
        {
            var best = 0;

            foreach (var effect in definition.Effects)
            {
                if (effect.Trigger != EffectTrigger.OnPlay) continue;
                if (effect is not DealDamageEffectDefinition damage || damage.Self) continue;

                if (damage.Amount > best) best = damage.Amount;
            }

            return best;
        }

        public static int PowerOf(CardDefinition definition)
        {
            return definition is UnitDefinition unit ? unit.Power : 0;
        }
    }
}
