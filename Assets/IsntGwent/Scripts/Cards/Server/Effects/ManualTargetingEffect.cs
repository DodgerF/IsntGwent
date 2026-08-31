using System;
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
        {
            var definition = (ManualTargetingDefinition)context.Definition;
            var available = context.ManualTargets.Count - context.ManualCursor;
            if (available <= 0) return new List<UnitInstance>();

            var take = definition.Count <= 0 ? available : Math.Min(definition.Count, available);
            var slice = context.ManualTargets.GetRange(context.ManualCursor, take);
            context.ManualCursor += take;

            return slice;
        }
    }

    public class ManualTargetingDefinition : TargetingEffectDefinition
    {
        public int Count;
        public bool AllowPartial;
    }
}
