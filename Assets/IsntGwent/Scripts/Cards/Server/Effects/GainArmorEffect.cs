using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class GainArmorEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (GainArmorDefinition)context.Definition;

            foreach (var target in ResolveApplyTargets(context))
                target.Armor.Value += definition.Amount;
        }

        private static List<UnitInstance> ResolveApplyTargets(EffectContext context)
        {
            if (context.Targets.Count > 0) return context.Targets;
            if (((GainArmorDefinition)context.Definition).RequireTargets) return new List<UnitInstance>();

            return context.Source is UnitInstance self
                ? new List<UnitInstance> { self }
                : new List<UnitInstance>();
        }
    }

    public class GainArmorDefinition : EffectDefinition
    {
        public int Amount;
        public bool RequireTargets;
    }
}
