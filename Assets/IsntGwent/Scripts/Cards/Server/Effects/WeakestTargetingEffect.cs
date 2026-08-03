using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using UnityEngine;

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
            var weakest = pool.Where(u => u.CurrentPower.Value == min).ToList();

            var count = ((WeakestTargetingDefinition)context.Definition).Count;
            if (count <= 0 || count >= weakest.Count) return weakest;

            var result = new List<UnitInstance>();

            while (result.Count < count)
            {
                var index = Random.Range(0, weakest.Count);

                result.Add(weakest[index]);
                weakest.RemoveAt(index);
            }

            return result;
        }
    }

    public class WeakestTargetingDefinition : TargetingEffectDefinition
    {
        public int Count;
    }
}
