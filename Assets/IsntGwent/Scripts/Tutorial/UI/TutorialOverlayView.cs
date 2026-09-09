using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Localization;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Tutorial.UI
{
    public class TutorialOverlayView : MonoBehaviour
    {
        public const int AuraSortingOrder = 202;
        public const int PanelSortingOrder = 210;

        [SerializeField] private RectTransform root;
        [SerializeField] private CanvasGroup group;
        [SerializeField] private RectTransform blocker;
        [SerializeField] private Image blockerImage;
        [SerializeField] private RectTransform textPanel;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private Button tapCatcher;

        [SerializeField] private float panelGap = 60f;
        [SerializeField] private float fadeDuration = 0.2f;

        public readonly Subject<Unit> Tapped = new();

        private string _source = string.Empty;
        private IReadOnlyList<RectTransform> _anchors;
        private bool _tapToAdvance;
        private bool _isOpen;
        private bool _isReady;

        private string _restoreSource;
        private IReadOnlyList<RectTransform> _panelAnchors;
        private IReadOnlyList<RectTransform> _restoreAnchors;
        private bool _restoreTap;
        private bool _hasRestore;
        private RectTransform _panelLine;
        private bool _hasPanelLine;
        private Tween _messageTimer;

        public void Bind(RectTransform overlayRoot, CanvasGroup canvasGroup, RectTransform tapArea,
            RectTransform panel, TextMeshProUGUI label, Button catcher)
        {
            root = overlayRoot;
            group = canvasGroup;
            blocker = tapArea;
            blockerImage = tapArea == null ? null : tapArea.GetComponent<Image>();
            textPanel = panel;
            text = label;
            tapCatcher = catcher;

            Init();
        }

        private void Awake() => Init();

        private void Init()
        {
            if (_isReady) return;
            if (root == null && blocker == null) return;

            _isReady = true;

            if (root == null) root = (RectTransform)transform;

            if (tapCatcher != null)
                tapCatcher.onClick.AddListener(() => Tapped.OnNext(Unit.Default));

            Loc.Language
                .Subscribe(_ => Retranslate())
                .AddTo(this);

            SetVisible(false);
        }

        public void Show(string source, IReadOnlyList<RectTransform> anchors, bool tapToAdvance)
            => Show(source, anchors, null, tapToAdvance);

        public void Show(string source, IReadOnlyList<RectTransform> anchors,
            IReadOnlyList<RectTransform> panelAnchors, bool tapToAdvance)
        {
            CancelMessage();

            _source = source ?? string.Empty;
            _anchors = anchors;
            _panelAnchors = panelAnchors;
            _tapToAdvance = tapToAdvance;

            SetVisible(true);

            Retranslate();
            PlacePanel(anchors, panelAnchors);

            if (tapCatcher != null)
                tapCatcher.interactable = tapToAdvance;

            if (blockerImage != null)
                blockerImage.raycastTarget = tapToAdvance;

            if (_isOpen || group == null) return;

            _isOpen = true;

            group.DOKill();
            group.alpha = 0f;
            group.DOFade(1f, fadeDuration).SetLink(gameObject);
        }

        public void ShowMessage(string source, float seconds)
        {
            _restoreSource = _isOpen ? _source : null;
            _restoreAnchors = _anchors;
            _restoreTap = _tapToAdvance;
            _hasRestore = true;

            Show(source, null, false);

            _messageTimer = DOVirtual.DelayedCall(seconds, Restore).SetLink(gameObject);
        }

        public void Hide()
        {
            CancelMessage();

            _anchors = null;

            if (!_isOpen || group == null)
            {
                SetVisible(false);
                return;
            }

            _isOpen = false;

            group.DOKill();
            group.DOFade(0f, fadeDuration)
                .SetLink(gameObject)
                .OnComplete(() => SetVisible(false));
        }

        private void Restore()
        {
            _messageTimer = null;

            if (!_hasRestore) return;

            _hasRestore = false;

            if (string.IsNullOrEmpty(_restoreSource))
            {
                Hide();
                return;
            }

            Show(_restoreSource, _restoreAnchors, _panelAnchors, _restoreTap);
        }

        private void CancelMessage()
        {
            if (_messageTimer == null) return;

            _messageTimer.Kill();
            _messageTimer = null;
            _hasRestore = false;
        }

        private void SetVisible(bool isVisible)
        {
            if (root != null) root.gameObject.SetActive(isVisible);

            if (isVisible) return;

            _isOpen = false;

            if (blockerImage != null) blockerImage.raycastTarget = false;
        }

        private void Retranslate()
        {
            if (text == null) return;

            text.text = string.IsNullOrEmpty(_source) ? string.Empty : Loc.T(_source);
        }

        private void PlacePanel(IReadOnlyList<RectTransform> anchors, IReadOnlyList<RectTransform> panelAnchors)
        {
            if (textPanel == null) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(textPanel);

            var half = root.rect.size * 0.5f;
            var panel = textPanel.rect.size;

            if (TryUnion(panelAnchors, out var seat))
            {
                var above = seat.yMax + panelGap + panel.y * 0.5f;
                var below = seat.yMin - panelGap - panel.y * 0.5f;
                var fits = above <= half.y - panel.y * 0.5f;

                textPanel.anchoredPosition = new Vector2(
                    Mathf.Clamp(seat.center.x, -half.x + panel.x * 0.5f, half.x - panel.x * 0.5f),
                    Clamp(fits ? above : below, panel.y, half.y));

                Pop();
                return;
            }

            var hasLine = TryPanelLine(out var lineY);

            if (!TryUnion(anchors, out var area))
            {
                textPanel.anchoredPosition = new Vector2(0f, hasLine ? Clamp(lineY, panel.y, half.y) : 0f);
                Pop();
                return;
            }

            float y;

            if (hasLine && !Overlaps(area, lineY, panel.y))
            {
                y = lineY;
            }
            else
            {
                var spaceAbove = half.y - area.yMax;
                var spaceBelow = area.yMin + half.y;

                y = spaceAbove >= spaceBelow
                    ? area.yMax + panelGap + panel.y * 0.5f
                    : area.yMin - panelGap - panel.y * 0.5f;
            }

            y = Clamp(y, panel.y, half.y);

            var x = Mathf.Clamp(area.center.x, -half.x + panel.x * 0.5f, half.x - panel.x * 0.5f);

            textPanel.anchoredPosition = new Vector2(x, y);

            Pop();
        }

        private static float Clamp(float y, float panelHeight, float halfHeight)
        {
            return Mathf.Clamp(y, -halfHeight + panelHeight * 0.5f, halfHeight - panelHeight * 0.5f);
        }

        private static bool Overlaps(Rect area, float lineY, float panelHeight)
        {
            return area.yMax > lineY - panelHeight * 0.5f && area.yMin < lineY + panelHeight * 0.5f;
        }

        private bool TryPanelLine(out float y)
        {
            y = 0f;

            if (!_hasPanelLine)
            {
                _hasPanelLine = true;

                var row = FindObjectsByType<BoardRowView>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                    .FirstOrDefault(r => !r.OwnSide && r.BoardRow == RowType.Melee);

                _panelLine = row == null ? null : (RectTransform)row.transform;
            }

            if (_panelLine == null) return false;

            y = LocalRect(_panelLine).center.y;

            return true;
        }

        private void Pop()
        {
            textPanel.DOKill();
            textPanel.localScale = Vector3.one * 0.92f;
            textPanel.DOScale(1f, fadeDuration).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        private bool TryUnion(IReadOnlyList<RectTransform> anchors, out Rect area)
        {
            area = new Rect();

            if (anchors == null) return false;

            var hasAny = false;
            var min = Vector2.zero;
            var max = Vector2.zero;

            foreach (var anchor in anchors)
            {
                if (anchor == null) continue;

                var rect = LocalRect(anchor);

                if (!hasAny)
                {
                    min = rect.min;
                    max = rect.max;
                    hasAny = true;
                    continue;
                }

                min = Vector2.Min(min, rect.min);
                max = Vector2.Max(max, rect.max);
            }

            if (!hasAny) return false;

            area = new Rect(min, max - min);

            return true;
        }

        private Rect LocalRect(RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            var min = root.InverseTransformPoint(corners[0]);
            var max = root.InverseTransformPoint(corners[2]);

            return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
        }
    }
}
