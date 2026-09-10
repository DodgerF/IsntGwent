using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;

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

            if (!string.IsNullOrEmpty(condition.Environment))
            {
                var present = HasEnvironment(context, condition.Environment);
                if (present == condition.EnvironmentAbsent) return false;
            }

            if (condition.Leadership != LeadershipMode.Any)
            {
                var held = context.Game != null && context.Game.HasLeadership(context.Owner, context.Source);
                if (held != (condition.Leadership == LeadershipMode.Held)) return false;
            }

            if (condition.MinSourcePower > 0)
            {
                if (context.Source is not UnitInstance source) return false;
                if (source.CurrentPower.Value < condition.MinSourcePower) return false;
            }

            return true;
        }

        private static bool HasEnvironment(EffectContext context, string cardId)
        {
            if (context.Game == null || context.Owner == null) return false;

            var opponent = context.Game.GetOpponent(context.Owner);
            if (opponent == null) return false;

            return HasWeather(opponent, RowType.Melee, cardId) || HasWeather(opponent, RowType.Ranged, cardId);
        }

        private static bool HasWeather(Player player, RowType row, string cardId)
        {
            var weather = player.GetWeather(row);

            return weather != null && weather.CardId == cardId;
        }
    }
}
