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

        private RectTransform _frame;
        private float _fullLeft;
        private float _fullRight;
        private float _viewportInset;
        private float _targetWidth;
        private bool _captured;
        private bool _hasTarget;
        private bool _applying;

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange() => Apply();

        private void LateUpdate()
        {
            if (!Application.isPlaying || !_hasTarget) return;

            ApplyWidth();
        }

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
            Capture();

            var padding = grid.padding;

            var peek = Mathf.Max(0f, peekRows);
            var gaps = peek > 0f ? visibleRows : visibleRows - 1;

            var width = FullFrameWidth() - _viewportInset - padding.left - padding.right - spacing.x * (columns - 1);
            var height = viewport.rect.height - padding.top - padding.bottom - spacing.y * gaps;

            if (width <= 0f || height <= 0f) return;

            var cellHeight = height / (visibleRows + peek);
            var cellWidth = Mathf.Min(width / columns, cellHeight / cellAspect);

            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = columns;
            grid.spacing = spacing;
            grid.cellSize = new Vector2(cellWidth, cellWidth * cellAspect);

            _targetWidth = padding.left + padding.right + cellWidth * columns + spacing.x * (columns - 1) + _viewportInset;
            _hasTarget = true;
        }

        private void Capture()
        {
            if (_captured) return;

            _captured = true;
            _frame = (RectTransform)transform;
            _fullLeft = _frame.offsetMin.x;
            _fullRight = _frame.offsetMax.x;
            _viewportInset = _frame.rect.width - viewport.rect.width;
        }

        private float FullFrameWidth() => AnchorWidth() + _fullRight - _fullLeft;

        private float AnchorWidth()
        {
            var parent = _frame.parent as RectTransform;
            var parentWidth = parent != null ? parent.rect.width : _frame.rect.width;

            return (_frame.anchorMax.x - _frame.anchorMin.x) * parentWidth;
        }

        private void ApplyWidth()
        {
            var full = FullFrameWidth();
            var side = (full - Mathf.Min(_targetWidth, full)) * 0.5f;

            var left = _fullLeft + side;
            var right = _fullRight - side;
            var half = _viewportInset * 0.5f;

            var frameMoved = Mathf.Abs(_frame.offsetMin.x - left) > 0.5f || Mathf.Abs(_frame.offsetMax.x - right) > 0.5f;
            var viewportMoved = Mathf.Abs(viewport.offsetMin.x - half) > 0.5f || Mathf.Abs(viewport.offsetMax.x + half) > 0.5f;

            if (!frameMoved && !viewportMoved) return;

            _applying = true;

            try
            {
                if (viewportMoved)
                {
                    viewport.offsetMin = new Vector2(half, viewport.offsetMin.y);
                    viewport.offsetMax = new Vector2(-half, viewport.offsetMax.y);
                }

                if (frameMoved)
                {
                    _frame.offsetMin = new Vector2(left, _frame.offsetMin.y);
                    _frame.offsetMax = new Vector2(right, _frame.offsetMax.y);
                }
            }
            finally
            {
                _applying = false;
            }
        }
    }
}
