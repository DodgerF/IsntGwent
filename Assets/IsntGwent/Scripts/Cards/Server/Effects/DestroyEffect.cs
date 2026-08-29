using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class DestroyEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            foreach (var target in context.Targets)
            {
                var power = target.CurrentPower.Value;
                if (power <= 0) continue;

                context.Game.RecordDamage(context.Source, target, power, context.DamageKind);
                target.GetDamage(power);
            }
        }
    }

    public class DestroyEffectDefinition : EffectDefinition
    {
    }
}
