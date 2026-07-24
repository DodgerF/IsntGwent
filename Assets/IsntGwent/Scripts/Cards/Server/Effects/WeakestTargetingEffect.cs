using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class WeakestTargetingEffect : TargetingEffect
    {
        public override bool NeedsManualTargets(EffectDefinition definition) => false;

        public override List<UnitInstance> GetPool(EffectContext context) => BuildPool(context);

        public override List<UnitInstance> ResolveTargets(EffectContext context)
        {
            var pool = GetPool(context);
            if (pool.Count == 0) return new List<UnitInstance>();

            var min = pool.Min(u => u.CurrentPower.Value);
            return pool.Where(u => u.CurrentPower.Value == min).ToList();
        }
    }

    public class WeakestTargetingDefinition : TargetingEffectDefinition
    {
    }
}
