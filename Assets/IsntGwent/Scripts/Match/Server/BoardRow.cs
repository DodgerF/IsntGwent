using System.Collections;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Match.Server
{
    public class BoardRow : IEnumerable<UnitInstance>
    {
        public readonly RowType RowType;
        public readonly BoardSlot[] Slots;

        public BoardRow(Player owner, RowType rowType)
        {
            RowType = rowType;
            Slots = new BoardSlot[BoardConfig.SlotsPerRow];

            for (var i = 0; i < Slots.Length; i++)
                Slots[i] = new BoardSlot(owner, rowType, i);
        }

        public int Count
        {
            get
            {
                var count = 0;
                foreach (var slot in Slots)
                    if (!slot.IsEmpty)
                        count++;
                return count;
            }
        }

        public bool IsFull => FirstFreeIndex < 0;

        public int FirstFreeIndex
        {
            get
            {
                for (var i = 0; i < Slots.Length; i++)
                    if (Slots[i].IsEmpty)
                        return i;
                return -1;
            }
        }

        public bool IsFree(int index) => BoardConfig.IsValidSlot(index) && Slots[index].IsEmpty;

        public bool TryPlace(UnitInstance unit, int index)
        {
            if (!IsFree(index)) return false;

            Slots[index].Unit = unit;
            return true;
        }

        public BoardSlot SlotOf(UnitInstance unit)
        {
            foreach (var slot in Slots)
                if (slot.Unit == unit)
                    return slot;

            return null;
        }

        public bool Remove(UnitInstance unit)
        {
            var slot = SlotOf(unit);
            if (slot == null) return false;

            slot.Unit = null;
            return true;
        }

        public bool Contains(UnitInstance unit) => SlotOf(unit) != null;

        public void Clear()
        {
            foreach (var slot in Slots)
                slot.Unit = null;
        }

        public IEnumerator<UnitInstance> GetEnumerator()
        {
            foreach (var slot in Slots)
                if (!slot.IsEmpty)
                    yield return slot.Unit;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
