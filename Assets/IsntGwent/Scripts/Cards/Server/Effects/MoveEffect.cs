using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Messages;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class MoveEffect : CardEffectBase
    {
        public override void Execute(EffectContext context)
        {
            var definition = (MoveEffectDefinition)context.Definition;

            if (definition.Subject == MoveSubject.Source)
            {
                MoveSource(context, definition);
                return;
            }

            var anchor = context.Anchor(definition.Anchor);
            var moves = new List<UnitMoved>();

            foreach (var target in context.Targets)
                Move(context, target, definition, anchor, moves);

            Publish(context, moves);
        }

        private static void MoveSource(EffectContext context, MoveEffectDefinition definition)
        {
            if (context.Source is not UnitInstance source) return;
            if (context.Targets.Count == 0) return;

            var from = context.Game.FindSlot(source);
            var target = context.Game.FindSlot(context.Targets[0]);
            if (from == null || target == null) return;

            var to = from.Owner.GetRow(from.Row).Slots[target.Index];

            var moves = new List<UnitMoved>();
            Place(context, source, from, to, moves);
            Publish(context, moves);
        }

        private static void Publish(EffectContext context, List<UnitMoved> moves)
        {
            foreach (var moved in moves)
                context.Game.Publish(moved);
        }

        private static void Move(EffectContext context, UnitInstance target,
            MoveEffectDefinition definition, BoardSlot anchor, List<UnitMoved> moves)
        {
            var from = context.Game.FindSlot(target);
            if (from == null) return;

            var to = definition.Axis == MoveAxis.Depth
                ? DepthDestination(context, definition, from, anchor)
                : RowDestination(definition, from, anchor);

            var destination = to;
            if (!Place(context, target, from, to, moves)) return;

            if (from.Owner != context.Owner)
                context.Game.RecordLink(context.Source, target, UnitLinkKind.Lure,
                    destination.Row, destination.Index);
        }

        private static bool Place(EffectContext context, UnitInstance unit, BoardSlot from, BoardSlot to,
            List<UnitMoved> moves)
        {
            if (to == null || !to.IsEmpty || to == from) return false;

            from.Unit = null;
            to.Unit = unit;
            unit.RowType = to.Row;

            context.Game.MarkBoardDirty();
            moves.Add(new UnitMoved(unit, to.Owner, from.Row, from.Index, to.Row, to.Index));
            return true;
        }

        private static BoardSlot RowDestination(MoveEffectDefinition definition, BoardSlot from, BoardSlot anchor)
        {
            var step = ResolveStep(definition, from, anchor);
            if (step == 0) return null;

            return step < 0 ? from.Left : from.Right;
        }

        private static BoardSlot DepthDestination(EffectContext context, MoveEffectDefinition definition,
            BoardSlot from, BoardSlot anchor)
        {
            var to = BoardGeometry.OtherRow(from);
            if (to == null) return null;

            if (definition.Direction == MoveDirection.OtherRow) return to;

            if (anchor == null) return null;
            if (definition.Direction is MoveDirection.Left or MoveDirection.Right) return null;

            var anchorLine = BoardGeometry.Line(anchor, context.Owner);
            var fromDepth = BoardGeometry.Depth(anchorLine, BoardGeometry.Line(from, context.Owner));
            var toDepth = BoardGeometry.Depth(anchorLine, BoardGeometry.Line(to, context.Owner));

            var closer = toDepth < fromDepth;
            return closer == (definition.Direction == MoveDirection.TowardSource) ? to : null;
        }

        private static int ResolveStep(MoveEffectDefinition definition, BoardSlot from, BoardSlot anchor)
        {
            switch (definition.Direction)
            {
                case MoveDirection.Left:
                    return -1;

                case MoveDirection.Right:
                    return 1;

                default:
                    if (anchor == null || anchor.Index == from.Index) return 0;

                    var toward = anchor.Index < from.Index ? -1 : 1;
                    return definition.Direction == MoveDirection.TowardSource ? toward : -toward;
            }
        }
    }

    public enum MoveDirection { TowardSource, AwayFromSource, Left, Right, OtherRow }

    public enum MoveAxis { Row, Depth }

    public enum MoveSubject { Target, Source }

    public class MoveEffectDefinition : EffectDefinition
    {
        public MoveDirection Direction;
        public MoveAxis Axis;
        public SlotAnchor Anchor;
        public MoveSubject Subject;
    }
}
