using IsntGwent.Scripts.Cards.Definitions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Decks.UI
{
    public class CardTooltipView : MonoBehaviour
    {
        public GameObject panel;
        public RectTransform panelRect;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descriptionText;
        public GameObject descriptionBlock;
        public float gap = 16f;

        private readonly Vector3[] _corners = new Vector3[4];

        private void Awake()
        {
            Hide();
        }

        public void Show(CardDefinition definition, RectTransform target)
        {
            if (definition == null || target == null) return;

            if (nameText != null)
                nameText.text = definition.Name;

            var hasDescription = !string.IsNullOrEmpty(definition.Description);

            if (descriptionText != null)
                descriptionText.text = definition.Description;

            if (descriptionBlock != null)
                descriptionBlock.SetActive(hasDescription);

            panel.SetActive(true);

            if (panelRect == null) return;

            Place(target);
        }

        public void Hide()
        {
            if (panel != null)
                panel.SetActive(false);
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

            var toLeft = bottomLeft.x - gap - size.x >= area.xMin;

            var centerX = toLeft
                ? Clamp(bottomLeft.x - gap, area.xMin + size.x, area.xMax)
                : Clamp(topRight.x + gap, area.xMin, area.xMax - size.x);

            panelRect.pivot = new Vector2(toLeft ? 1f : 0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(centerX, centerY);
        }

        private static float Clamp(float value, float min, float max) =>
            min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);
    }
}
