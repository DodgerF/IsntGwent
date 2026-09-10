using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class HealEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (HealDefinition)context.Definition;

            foreach (var target in ResolveApplyTargets(context))
            {
                var basePower = target.BasePower.Value;
                if (target.CurrentPower.Value >= basePower) continue;

                var restored = definition.Amount <= 0
                    ? basePower
                    : Math.Min(basePower, target.CurrentPower.Value + definition.Amount);

                target.CurrentPower.Value = restored;
            }
        }

        private static List<UnitInstance> ResolveApplyTargets(EffectContext context)
        {
            if (context.Targets.Count > 0) return context.Targets;
            if (((HealDefinition)context.Definition).RequireTargets) return new List<UnitInstance>();

            return context.Source is UnitInstance self
                ? new List<UnitInstance> { self }
                : new List<UnitInstance>();
        }
    }

    public class HealDefinition : EffectDefinition
    {
        public int Amount;
        public bool RequireTargets;
    }
}
