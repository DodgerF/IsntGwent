using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;
using UnityEngine;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class NeighborTargetingEffect : TargetingEffect
    {
        public override bool NeedsManualTargets(EffectDefinition definition) => false;

        public override List<UnitInstance> GetPool(EffectContext context)
        {
            var definition = (NeighborTargetingDefinition)context.Definition;

            var anchor = definition.RelativeTo == NeighborRelativeTo.ManualTarget
                ? context.ManualTargets.FirstOrDefault()
                : context.Source as UnitInstance;

            if (anchor == null) return new List<UnitInstance>();

            var (left, right) = context.Game.FindSlot(anchor) != null
                ? BoardSyncService.FindNeighbors(context.Game, anchor)
                : NeighborsOfDead(context, anchor);

            var neighbors = new List<UnitInstance>();
            if (left != null) neighbors.Add(left);
            if (right != null) neighbors.Add(right);

            return neighbors;
        }

        private static (UnitInstance Left, UnitInstance Right) NeighborsOfDead(EffectContext context,
            UnitInstance anchor)
        {
            if (context.Event is not UnitDied died || died.Unit != anchor) return (null, null);

            return (died.LeftNeighbor, died.RightNeighbor);
        }

        public override List<UnitInstance> ResolveTargets(EffectContext context)
        {
            var definition = (NeighborTargetingDefinition)context.Definition;
            var neighbors = GetPool(context);

            if (neighbors.Count == 0 || definition.Pick == NeighborPick.All)
                return neighbors;

            var edge = definition.Pick == NeighborPick.Highest
                ? neighbors.Max(u => u.CurrentPower.Value)
                : neighbors.Min(u => u.CurrentPower.Value);

            var candidates = neighbors.Where(u => u.CurrentPower.Value == edge).ToList();
            var chosen = candidates[Random.Range(0, candidates.Count)];

            return new List<UnitInstance> { chosen };
        }
    }

    public enum NeighborRelativeTo { Source, ManualTarget }
    public enum NeighborPick { All, Highest, Lowest }

    public class NeighborTargetingDefinition : TargetingEffectDefinition
    {
        public NeighborRelativeTo RelativeTo;
        public NeighborPick Pick;
    }
}
