using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class ApplyWeatherEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            if (context.Owner == null || context.PlayedRow == RowType.None) return;

            var target = context.TargetsEnemyRow
                ? context.Game.GetOpponent(context.Owner)
                : context.Owner;

            target.AddWeather(context.PlayedRow, context.Source);
            context.Game.Journal?.Weather(target, context.PlayedRow, context.Source);
            context.Game.MarkBoardDirty();
        }
    }

    public class ApplyWeatherDefinition : EffectDefinition
    {
    }
}
