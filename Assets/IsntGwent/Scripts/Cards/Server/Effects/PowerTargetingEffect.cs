using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using UnityEngine;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public abstract class PowerTargetingEffect : TargetingEffect
    {
        protected abstract bool PickHighest { get; }

        public override bool NeedsManualTargets(EffectDefinition definition) => false;

        public override List<UnitInstance> GetPool(EffectContext context) => BuildPool(context);

        public override List<UnitInstance> ResolveTargets(EffectContext context)
        {
            var pool = GetPool(context);
            if (pool.Count == 0) return new List<UnitInstance>();

            var edge = PickHighest
                ? pool.Max(u => u.CurrentPower.Value)
                : pool.Min(u => u.CurrentPower.Value);

            var candidates = pool.Where(u => u.CurrentPower.Value == edge).ToList();

            var count = ((PowerTargetingDefinition)context.Definition).Count;
            if (count <= 0 || count >= candidates.Count) return candidates;

            var result = new List<UnitInstance>();

            while (result.Count < count)
            {
                var index = Random.Range(0, candidates.Count);

                result.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            return result;
        }
    }

    public abstract class PowerTargetingDefinition : TargetingEffectDefinition
    {
        public int Count;
    }
}
