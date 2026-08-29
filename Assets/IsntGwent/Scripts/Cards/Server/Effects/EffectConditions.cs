using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public static class EffectConditions
    {
        public static bool IsMet(EffectContext context, EffectDefinition definition)
        {
            var condition = definition.Condition;
            if (condition == null) return true;

            if (!string.IsNullOrEmpty(condition.AllyOnBoard))
            {
                var present = context.Game != null && context.Game.HasOnBoard(context.Owner, condition.AllyOnBoard);
                if (present == condition.AllyAbsent) return false;
            }

            if (condition.Leadership != LeadershipMode.Any)
            {
                var held = context.Game != null && context.Game.HasLeadership(context.Owner, context.Source);
                if (held != (condition.Leadership == LeadershipMode.Held)) return false;
            }

            return true;
        }
    }
}
