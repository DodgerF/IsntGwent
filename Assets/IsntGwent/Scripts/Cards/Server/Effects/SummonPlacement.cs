using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public enum SummonPlacement { TargetRow, Neighbors, EachRow, OwnPosition }

    public static class SummonUtil
    {
        public static bool Place(EffectContext context, UnitInstance unit,
            SummonPlacement placement, RowType targetRow, int index, bool fromDeck,
            bool strictNeighbors = false)
        {
            var slot = Resolve(context, placement, targetRow, index, strictNeighbors);
            if (slot == null && !strictNeighbors) slot = context.Owner.FirstFreeSlot();
            if (slot == null) return false;

            unit.RowType = slot.Row;
            slot.Unit = unit;

            context.Game.OnUnitAddedToRow(unit);
            context.Game.MarkBoardDirty();
            context.Game.Publish(new UnitSummoned(unit, context.Owner, fromDeck));
            return true;
        }

        private static BoardSlot Resolve(EffectContext context, SummonPlacement placement,
            RowType targetRow, int index, bool strictNeighbors)
        {
            var source = context.Source as UnitInstance;
            var fallbackRow = targetRow != RowType.None
                ? targetRow
                : source?.RowType ?? (context.PlayedRow != RowType.None ? context.PlayedRow : RowType.Melee);

            switch (placement)
            {
                case SummonPlacement.EachRow:
                    return FirstFreeIn(context, index % 2 == 0 ? RowType.Melee : RowType.Ranged);

                case SummonPlacement.Neighbors:
                    return ResolveNeighbors(context, source, strictNeighbors);

                case SummonPlacement.OwnPosition:
                    return ResolveOwnPosition(context, fallbackRow);

                default:
                    return FirstFreeIn(context, fallbackRow);
            }
        }

        private static BoardSlot FirstFreeIn(EffectContext context, RowType rowType)
        {
            var row = context.Owner.GetRow(rowType);
            var index = row.FirstFreeIndex;
            return index >= 0 ? row.Slots[index] : null;
        }

        private static BoardSlot ResolveNeighbors(EffectContext context, UnitInstance source, bool strict)
        {
            if (source == null) return null;

            var anchor = context.Owner.FindSlot(source);
            if (anchor == null) return null;

            var row = context.Owner.GetRow(anchor.Row);
            var maxDistance = strict ? 2 : BoardConfig.SlotsPerRow;

            for (var distance = 1; distance < maxDistance; distance++)
            {
                var right = anchor.Index + distance;
                if (right < BoardConfig.SlotsPerRow && row.Slots[right].IsEmpty)
                    return row.Slots[right];

                var left = anchor.Index - distance;
                if (left >= 0 && row.Slots[left].IsEmpty)
                    return row.Slots[left];
            }

            return null;
        }

        private static BoardSlot ResolveOwnPosition(EffectContext context, RowType fallbackRow)
        {
            if (context.Event is UnitDied died && BoardConfig.IsValidSlot(died.SlotIndex))
            {
                var row = context.Owner.GetRow(died.Row);
                if (row.Slots[died.SlotIndex].IsEmpty)
                    return row.Slots[died.SlotIndex];
            }

            return FirstFreeIn(context, fallbackRow);
        }
    }
}
