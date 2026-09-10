using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class LineTargetingEffect : TargetingEffect
    {
        public override bool NeedsManualTargets(EffectDefinition definition) => false;

        public override List<UnitInstance> GetPool(EffectContext context)
        {
            var definition = (LineTargetingDefinition)context.Definition;
            var empty = new List<UnitInstance>();

            var anchor = context.Anchor(definition.Anchor);
            if (anchor == null) return empty;

            var opponent = context.Game?.GetOpponent(context.Owner);
            var anchorLine = BoardGeometry.Line(anchor, context.Owner);

            foreach (var line in BoardGeometry.LinesAhead(anchorLine))
            {
                var slot = BoardGeometry.SlotAt(context.Owner, opponent, line, anchor.Index);
                if (slot?.Unit == null) continue;

                return MatchesSide(definition, slot.Owner == context.Owner)
                    ? new List<UnitInstance> { slot.Unit }
                    : empty;
            }

            return empty;
        }

        public override List<UnitInstance> ResolveTargets(EffectContext context) => GetPool(context);
    }

    public class LineTargetingDefinition : TargetingEffectDefinition
    {
        public SlotAnchor Anchor;
    }
}
