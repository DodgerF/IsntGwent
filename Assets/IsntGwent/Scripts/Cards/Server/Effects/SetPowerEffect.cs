using System;
using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class SetPowerEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (SetPowerDefinition)context.Definition;
            var amount = Math.Max(0, definition.Amount);

            foreach (var target in context.Targets)
            {
                target.BasePower.Value = amount;

                if (target.CurrentPower.Value > amount)
                    target.CurrentPower.Value = amount;
            }
        }
    }

    public class SetPowerDefinition : EffectDefinition
    {
        public int Amount;
    }
}
