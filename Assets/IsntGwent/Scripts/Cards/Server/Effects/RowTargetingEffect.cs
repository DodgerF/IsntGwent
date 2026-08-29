using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class RowTargetingEffect : TargetingEffect
    {
        public override bool NeedsManualTargets(EffectDefinition definition) => false;

        public override List<UnitInstance> GetPool(EffectContext context) => BuildPool(context);

        public override List<UnitInstance> ResolveTargets(EffectContext context) => GetPool(context);
    }

    public class RowTargetingDefinition : TargetingEffectDefinition
    {
    }
}
