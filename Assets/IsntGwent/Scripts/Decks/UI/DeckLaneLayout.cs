using IsntGwent.Scripts.Cards.UI;
using UnityEngine;

namespace IsntGwent.Scripts.Decks.UI
{
    public static class DeckLaneLayout
    {
        public const int LaneCapacity = 9;

        private const float ReferenceLaneHeight = 160f;
        private const float ReferenceCardSpacing = 115f;
        private const float LaneGap = 30f;
        private const float LaneSideMargin = 20f;
        private const float LaneWidthReserve = 100f;

        private static readonly Vector2 Center = new(0.5f, 0.5f);
        private static readonly float[] LaneWidthFactors = { 1f, 1f, 0.9f };

        public static void Apply(RectTransform root, CardLaneView[] lanes)
        {
            if (root == null || lanes == null || lanes.Length == 0) return;

            var area = root.rect;
            if (area.width <= 0f || area.height <= 0f) return;

            var count = lanes.Length;
            var laneHeight = Mathf.Min(ReferenceLaneHeight, (area.height - LaneGap * (count - 1)) / count);
            if (laneHeight <= 0f) return;

            var scale = laneHeight / ReferenceLaneHeight;
            var step = laneHeight + LaneGap;
            var top = (count - 1) * step / 2f;
            var fullWidth = Mathf.Max(0f, area.width - LaneSideMargin * 2f);

            for (var i = 0; i < count; i++)
            {
                var lane = lanes[i];
                if (lane == null) continue;

                var factor = LaneWidthFactors[Mathf.Clamp(i, 0, LaneWidthFactors.Length - 1)];
                var laneWidth = fullWidth * factor;
                var rect = (RectTransform)lane.transform;

                rect.anchorMin = Center;
                rect.anchorMax = Center;
                rect.pivot = Center;
                rect.sizeDelta = new Vector2(laneWidth, laneHeight);
                rect.anchoredPosition = new Vector2(0f, top - i * step);
                rect.localScale = Vector3.one;

                lane.cardSpacing = ReferenceCardSpacing * scale;
                lane.cardScale = scale;
                lane.maxWidth = Mathf.Max(0f, laneWidth - LaneWidthReserve * scale);
                lane.RefreshLayout();
            }
        }
    }
}
