using IsntGwent.Scripts.Cards.UI;
using UnityEngine;

namespace IsntGwent.Scripts.Decks.UI
{
    public static class DeckLaneLayout
    {
        public const int LaneCapacity = 9;

        private const float CardSpacing = 115f;
        private const float LaneHeight = 160f;

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly Vector2 RootAnchorMin = new(0f, 0f);
        private static readonly Vector2 RootAnchorMax = new(0.5f, 1f);
        private static readonly Vector2 RootAnchoredPosition = new(5f, -56f);
        private static readonly Vector2 RootSizeDelta = new(-30f, -280f);

        private static readonly float[] LaneWidths = { 1000f, 1000f, 900f };
        private static readonly float[] LaneMaxWidths = { 900f, 900f, 800f };
        private static readonly float[] LaneOffsets = { 350f, 160f, -30f };

        public static void Apply(RectTransform root, CardLaneView[] lanes)
        {
            if (root != null)
            {
                root.anchorMin = RootAnchorMin;
                root.anchorMax = RootAnchorMax;
                root.pivot = Center;
                root.anchoredPosition = RootAnchoredPosition;
                root.sizeDelta = RootSizeDelta;
            }

            if (lanes == null) return;

            for (var i = 0; i < lanes.Length; i++)
            {
                var lane = lanes[i];
                if (lane == null) continue;

                var index = Mathf.Clamp(i, 0, LaneWidths.Length - 1);
                var rect = (RectTransform)lane.transform;

                rect.anchorMin = Center;
                rect.anchorMax = Center;
                rect.pivot = Center;
                rect.sizeDelta = new Vector2(LaneWidths[index], LaneHeight);
                rect.anchoredPosition = new Vector2(0f, LaneOffsets[index]);
                rect.localScale = Vector3.one;

                lane.cardSpacing = CardSpacing;
                lane.maxWidth = LaneMaxWidths[index];
                lane.RefreshLayout();
            }
        }
    }
}
