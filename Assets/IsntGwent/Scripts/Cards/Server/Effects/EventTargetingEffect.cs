using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class EventTargetingEffect : TargetingEffect
    {
        public override bool NeedsManualTargets(EffectDefinition definition) => false;

        public override List<UnitInstance> GetPool(EffectContext context)
        {
            var definition = (EventTargetingDefinition)context.Definition;
            var unit = UnitOf(context.Event);

            if (unit == null || unit == context.Source) return new List<UnitInstance>();

            var isAlly = context.Owner != null && context.Owner.FindSlot(unit) != null;

            if (isAlly && !definition.IncludeAllies) return new List<UnitInstance>();
            if (!isAlly && !definition.IncludeEnemies) return new List<UnitInstance>();

            return new List<UnitInstance> { unit };
        }

        public override List<UnitInstance> ResolveTargets(EffectContext context) => GetPool(context);

        private static UnitInstance UnitOf(IGameEvent gameEvent)
        {
            return gameEvent switch
            {
                UnitMoved moved => moved.Unit,
                UnitDied died => died.Unit,
                UnitSummoned summoned => summoned.Unit,
                UnitDevoured devoured => devoured.Consumed,
                CardPlayed played => played.Card as UnitInstance,
                _ => null
            };
        }
    }

    public class EventTargetingDefinition : TargetingEffectDefinition
    {
    }
}
