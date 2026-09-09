using IsntGwent.Scripts.Cards.UI;
using UnityEngine;

namespace IsntGwent.Scripts.Match.UI
{
    public class PendingPlayLaneView : CardLaneView
    {
        public float stackOffsetX = 18f;
        public float stackOffsetY = 14f;

        public bool Contains(GameObject card) => card != null && _cards.Contains(card.transform);

        public override float RaiseShift(Transform card, float lift, float t) => 0f;

        protected override void LayoutCards()
        {
            var count = _cards.Count;
            if (count == 0)
                return;

            for (var i = 0; i < count; i++)
            {
                var child = _cards[i];
                var isFlight = child == _flightChild;

                child.SetSiblingIndex(count - 1 - i);

                MoveTo(
                    child,
                    new Vector3(i * stackOffsetX, i * stackOffsetY, 0f),
                    isFlight ? _flightDuration : CardAnimConfig.RowLayoutDuration,
                    isFlight ? CardAnimConfig.FlightEase : CardAnimConfig.RowLayoutEase);
            }
        }
    }
}
