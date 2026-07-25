using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class DealDamageEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (DealDamageEffectDefinition)context.Definition;

            foreach (var target in context.Targets)
            {
                context.Game.RecordDamage(context.Source, target, definition.Amount);
                target.GetDamage(definition.Amount);
            }
        }
    }

    public class DealDamageEffectDefinition : EffectDefinition
    {
        public int Amount;
    }
}
