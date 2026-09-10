using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Decks.UI
{
    [ExecuteAlways]
    public class CollectionGridLayout : MonoBehaviour
    {
        public RectTransform viewport;
        public GridLayoutGroup grid;

        public int columns = 5;
        public int visibleRows = 3;
        public float peekRows = 0.35f;
        public Vector2 spacing = new(15f, 35f);
        public float cellAspect = 260f / 180f;

        private bool _applying;

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            if (_applying) return;
            if (viewport == null || grid == null) return;
            if (columns < 1 || visibleRows < 1) return;

            _applying = true;

            try
            {
                Rebuild();
            }
            finally
            {
                _applying = false;
            }
        }

        private void Rebuild()
        {
            var padding = grid.padding;

            var peek = Mathf.Max(0f, peekRows);
            var gaps = peek > 0f ? visibleRows : visibleRows - 1;

            var width = viewport.rect.width - padding.left - padding.right - spacing.x * (columns - 1);
            var height = viewport.rect.height - padding.top - padding.bottom - spacing.y * gaps;

            if (width <= 0f || height <= 0f) return;

            var cellHeight = height / (visibleRows + peek);
            var cellWidth = Mathf.Min(width / columns, cellHeight / cellAspect);

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.spacing = spacing;
            grid.cellSize = new Vector2(cellWidth, cellWidth * cellAspect);
        }
    }
}
