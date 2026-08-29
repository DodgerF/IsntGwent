using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Match
{
    public static class BoardGeometry
    {
        public const int LastLine = 3;

        public static int Line(bool ownSide, RowType row)
        {
            if (ownSide)
                return row == RowType.Ranged ? 0 : 1;

            return row == RowType.Melee ? 2 : 3;
        }

        public static int Line(BoardSlot slot, Player viewer) => Line(slot.Owner == viewer, slot.Row);

        public static int Depth(int fromLine, int toLine)
        {
            var lines = fromLine - toLine;
            return lines < 0 ? -lines : lines;
        }

        public static int Spread(int fromColumn, int toColumn)
        {
            var columns = fromColumn - toColumn;
            return columns < 0 ? -columns : columns;
        }

        public static IEnumerable<int> LinesAhead(int fromLine)
        {
            for (var line = fromLine + 1; line <= LastLine; line++)
                yield return line;
        }

        public static BoardSlot SlotAt(Player viewer, Player opponent, int line, int column)
        {
            if (!BoardConfig.IsValidSlot(column)) return null;

            var owner = line <= 1 ? viewer : opponent;
            if (owner == null) return null;

            var row = line == 0 || line == LastLine ? RowType.Ranged : RowType.Melee;

            return owner.GetRow(row).Slots[column];
        }

        public static BoardCell Cell(int line, int column)
        {
            var ownSide = line <= 1;
            var row = line == 0 || line == LastLine ? RowType.Ranged : RowType.Melee;

            return new BoardCell(ownSide, row, column);
        }

        public static BoardSlot OtherRow(BoardSlot slot)
        {
            if (slot == null) return null;

            var other = slot.Row == RowType.Melee ? RowType.Ranged : RowType.Melee;
            return slot.Owner.GetRow(other).Slots[slot.Index];
        }
    }
}
