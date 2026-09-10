using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Match.Server
{
    public class BoardSlot
    {
        public Player Owner;
        public RowType Row;
        public int Index;

        public UnitInstance Unit;

        public BoardSlot Left;
        public BoardSlot Right;
        public BoardSlot Opposite;

        public bool IsEmpty => Unit == null;

        public BoardSlot(Player owner, RowType row, int index)
        {
            Owner = owner;
            Row = row;
            Index = index;
        }
    }
}
