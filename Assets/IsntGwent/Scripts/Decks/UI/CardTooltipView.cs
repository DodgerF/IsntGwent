using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Decks.UI
{
    public class CardTooltipView : MonoBehaviour
    {
        public GameObject panel;
        public RectTransform panelRect;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public GameObject descriptionBlock;
        public Cards.UI.KeywordListView keywords;
        public float gap = 16f;

        [Inject] private KeywordDatabase _keywords;

        private readonly Vector3[] _corners = new Vector3[4];

        private void Awake()
        {
            EnsureLayout();
            Hide();
        }

        private void EnsureLayout()
        {
            if (panelRect != null)
            {
                var group = FitHeightToChildren(panelRect.gameObject);
                group.padding = new RectOffset(26, 26, 22, 24);
                group.spacing = 12f;

                var fitter = panelRect.GetComponent<ContentSizeFitter>();
                if (fitter == null) fitter = panelRect.gameObject.AddComponent<ContentSizeFitter>();

                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            if (descriptionBlock != null)
            {
                var group = FitHeightToChildren(descriptionBlock);
                group.padding = new RectOffset(0, 0, 0, 0);
                group.spacing = 8f;
            }
        }

        private static VerticalLayoutGroup FitHeightToChildren(GameObject target)
        {
            var group = target.GetComponent<VerticalLayoutGroup>();
            if (group == null) group = target.AddComponent<VerticalLayoutGroup>();

            group.childControlHeight = true;
            group.childForceExpandHeight = false;
            group.childControlWidth = true;
            group.childForceExpandWidth = true;

            return group;
        }

        public void Show(CardDefinition definition, RectTransform target)
        {
            if (definition == null || target == null) return;

            if (nameText != null)
                nameText.text = definition.Name;

            var hasDescription = !string.IsNullOrWhiteSpace(definition.Description);

            if (descriptionText != null)
                descriptionText.text = _keywords.Format(definition.Description);

            if (descriptionBlock != null)
                descriptionBlock.SetActive(hasDescription);

            panel.SetActive(true);

            if (keywords != null)
                keywords.Show(definition.Description);

            if (panelRect == null) return;

            Place(target);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);

            if (keywords != null)
                keywords.Hide();
        }

        private void Place(RectTransform target)
        {
            if (panelRect.parent is not RectTransform parent) return;

            target.GetWorldCorners(_corners);

            var bottomLeft = parent.InverseTransformPoint(_corners[0]);
            var topRight = parent.InverseTransformPoint(_corners[2]);

            panelRect.anchorMin = parent.pivot;
            panelRect.anchorMax = parent.pivot;
            LayoutRebuilder.ForceRebuildLayoutImmediate(panelRect);

            var size = panelRect.rect.size;
            var area = parent.rect;

            var centerY = Clamp(
                (bottomLeft.y + topRight.y) * 0.5f,
                area.yMin + size.y * 0.5f,
                area.yMax - size.y * 0.5f);

            var keywordWidth = KeywordWidth();
            var needed = size.x + (keywordWidth > 0f ? gap + keywordWidth : 0f);

            var roomLeft = bottomLeft.x - gap - needed >= area.xMin;
            var roomRight = topRight.x + gap + needed <= area.xMax;

            var toLeft = roomLeft
                || !roomRight && bottomLeft.x - area.xMin > area.xMax - topRight.x;

            var centerX = toLeft
                ? Clamp(bottomLeft.x - gap, area.xMin + needed, area.xMax)
                : Clamp(topRight.x + gap, area.xMin, area.xMax - needed);

            panelRect.pivot = new Vector2(toLeft ? 1f : 0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(centerX, centerY);

            PlaceKeywords(toLeft, area, new Vector2(centerX + (0.5f - panelRect.pivot.x) * size.x, centerY));
        }

        private float KeywordWidth()
        {
            if (keywords == null || keywords.panel == null || !keywords.panel.activeSelf) return 0f;
            if (keywords.transform is not RectTransform rect) return 0f;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            return rect.rect.width;
        }

        private void PlaceKeywords(bool side, Rect area, Vector2 panelCenter)
        {
            if (keywords == null || keywords.panel == null || !keywords.panel.activeSelf) return;
            if (keywords.transform is not RectTransform rect) return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);

            var size = rect.rect.size;

            rect.anchorMin = new Vector2(side ? 0f : 1f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(side ? 1f : 0f, 0.5f);

            var centerY = Clamp(
                panelCenter.y,
                area.yMin + size.y * 0.5f,
                area.yMax - size.y * 0.5f);

            rect.anchoredPosition = new Vector2(side ? -gap : gap, centerY - panelCenter.y);
        }

        private static float Clamp(float value, float min, float max) =>
            min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
    }
}
