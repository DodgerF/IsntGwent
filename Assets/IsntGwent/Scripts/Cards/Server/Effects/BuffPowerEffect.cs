using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class BuffPowerEffect : CardEffectBase
    {
        public override bool CanTrigger(EffectContext context)
        {
            var definition = (BuffPowerDefinition)context.Definition;
            if (!definition.OnlyIfEventNeighbor) return true;

            if (context.Event is not UnitDied died) return false;
            return context.Source == died.LeftNeighbor || context.Source == died.RightNeighbor;
        }

        public override void Execute(EffectContext context)
        {
            var definition = (BuffPowerDefinition)context.Definition;
            var targets = ResolveApplyTargets(context);

            foreach (var target in targets)
                target.CurrentPower.Value += definition.Amount;
        }

        private static List<UnitInstance> ResolveApplyTargets(EffectContext context)
        {
            if (context.Targets.Count > 0) return context.Targets;
            if (((BuffPowerDefinition)context.Definition).RequireTargets) return new List<UnitInstance>();

            return context.Source is UnitInstance self
                ? new List<UnitInstance> { self }
                : new List<UnitInstance>();
        }
    }

    public class BuffPowerDefinition : EffectDefinition
    {
        public int Amount;
        public bool OnlyIfEventNeighbor;
        public bool RequireTargets;
    }
}
