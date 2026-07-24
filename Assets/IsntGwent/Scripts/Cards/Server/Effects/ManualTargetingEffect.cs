using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class ManualTargetingEffect : TargetingEffect
    {
        public override bool NeedsManualTargets(EffectDefinition definition) => true;

        public override int ManualTargetCount(EffectDefinition definition)
            => ((ManualTargetingDefinition)definition).Count;

        public override List<UnitInstance> GetPool(EffectContext context) => BuildPool(context);

        public override List<UnitInstance> ResolveTargets(EffectContext context)
            => context.ManualTargets;
    }

    public class ManualTargetingDefinition : TargetingEffectDefinition
    {
        public int Count;
        public bool AllowPartial;
    }
}
