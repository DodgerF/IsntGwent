using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using UnityEngine;

namespace IsntGwent.Scripts.Decks.UI
{
    public static class DeckCardOrder
    {
        public static readonly IComparer<CardDefinition> Comparer = new DefinitionComparer();

        public static int LaneOf(int index, int laneCapacity, int laneCount)
        {
            if (laneCount <= 1) return 0;

            return Mathf.Clamp(index / Mathf.Max(laneCapacity, 1), 0, laneCount - 1);
        }

        public static int PositionInLane(int index, int laneCapacity, int laneCount)
        {
            return index - LaneOf(index, laneCapacity, laneCount) * Mathf.Max(laneCapacity, 1);
        }

        private class DefinitionComparer : IComparer<CardDefinition>
        {
            public int Compare(CardDefinition a, CardDefinition b)
            {
                if (ReferenceEquals(a, b)) return 0;
                if (a == null) return 1;
                if (b == null) return -1;

                var byGroup = Group(a).CompareTo(Group(b));
                if (byGroup != 0) return byGroup;

                var byPower = Power(b).CompareTo(Power(a));
                if (byPower != 0) return byPower;

                return string.CompareOrdinal(a.Id, b.Id);
            }

            private static int Group(CardDefinition card) => card is UnitDefinition ? 0 : 1;

            private static int Power(CardDefinition card) => card is UnitDefinition unit ? unit.Power : 0;
        }
    }
}
