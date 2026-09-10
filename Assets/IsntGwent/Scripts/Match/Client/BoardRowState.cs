using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Runtime;
using UniRx;

namespace IsntGwent.Scripts.Match.Client
{
    public class BoardRowState
    {
        public readonly CardInstance[] Slots = new CardInstance[BoardConfig.SlotsPerRow];

        public readonly Subject<(int Index, CardInstance Card)> Placed = new();
        public readonly Subject<(int Index, CardInstance Card)> Removed = new();

        public IEnumerable<CardInstance> Units
        {
            get
            {
                foreach (var card in Slots)
                    if (card != null)
                        yield return card;
            }
        }

        public bool IsFree(int index) => BoardConfig.IsValidSlot(index) && Slots[index] == null;

        public int IndexOf(CardInstance card)
        {
            for (var i = 0; i < Slots.Length; i++)
                if (Slots[i] == card)
                    return i;

            return -1;
        }

        public bool Contains(CardInstance card) => IndexOf(card) >= 0;

        public void Place(int index, CardInstance card)
        {
            if (!BoardConfig.IsValidSlot(index) || card == null) return;

            Remove(card);

            var previous = Slots[index];
            if (previous != null)
                RemoveAt(index);

            Slots[index] = card;
            Placed.OnNext((index, card));
        }

        public bool Remove(CardInstance card)
        {
            var index = IndexOf(card);
            if (index < 0) return false;

            RemoveAt(index);
            return true;
        }

        public void Clear()
        {
            for (var i = 0; i < Slots.Length; i++)
                if (Slots[i] != null)
                    RemoveAt(i);
        }

        private void RemoveAt(int index)
        {
            var card = Slots[index];
            Slots[index] = null;
            Removed.OnNext((index, card));
        }
    }
}
