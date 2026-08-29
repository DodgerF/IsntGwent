using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Match
{
    public readonly struct BoardCell
    {
        public readonly bool OwnSide;
        public readonly RowType Row;
        public readonly int Index;

        public BoardCell(bool ownSide, RowType row, int index)
        {
            OwnSide = ownSide;
            Row = row;
            Index = index;
        }

        public int Line => BoardGeometry.Line(OwnSide, Row);

        public bool IsValid => Row != RowType.None && BoardConfig.IsValidSlot(Index);

        public bool Equals(BoardCell other)
            => OwnSide == other.OwnSide && Row == other.Row && Index == other.Index;
    }
}
