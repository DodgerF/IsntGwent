using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class SlotTargetingEffect : TargetingEffect
    {
        public override bool NeedsManualTargets(EffectDefinition definition) => false;

        public override List<UnitInstance> GetPool(EffectContext context)
        {
            var definition = (SlotTargetingDefinition)context.Definition;

            var anchor = context.Anchor(definition.Anchor);
            if (anchor == null) return new List<UnitInstance>();

            var slot = Step(anchor, definition.Direction);
            if (slot?.Unit == null) return new List<UnitInstance>();
            if (!MatchesSide(definition, slot.Owner == context.Owner)) return new List<UnitInstance>();

            return new List<UnitInstance> { slot.Unit };
        }

        public override List<UnitInstance> ResolveTargets(EffectContext context) => GetPool(context);

        private static BoardSlot Step(BoardSlot slot, SlotDirection direction)
        {
            return direction switch
            {
                SlotDirection.Opposite => slot.Opposite,
                SlotDirection.Left => slot.Left,
                SlotDirection.Right => slot.Right,
                SlotDirection.OppositeLeft => slot.Opposite?.Left,
                SlotDirection.OppositeRight => slot.Opposite?.Right,
                _ => null
            };
        }
    }

    public enum SlotAnchor { Source, ManualTarget }

    public enum SlotDirection { Opposite, Left, Right, OppositeLeft, OppositeRight }

    public class SlotTargetingDefinition : TargetingEffectDefinition
    {
        public SlotAnchor Anchor;
        public SlotDirection Direction;
    }
}
