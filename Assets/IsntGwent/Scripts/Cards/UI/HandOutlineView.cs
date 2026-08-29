using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Cards.UI
{
    [RequireComponent(typeof(CardLaneView))]
    public class HandOutlineView : MonoBehaviour
    {
        public float glowWidth = 26f;
        public Color color = new(0.42f, 0.76f, 1f, 0.8f);
        public float fadeDuration = 0.2f;
        public Material glowMaterial;

        private readonly List<Image> _quads = new();

        private CardLaneView _lane;
        private RectTransform _layer;
        private CanvasGroup _group;
        private bool _shown;

        private void Awake() => _lane = GetComponent<CardLaneView>();

        public void SetShown(bool shown)
        {
            if (_shown == shown) return;

            _shown = shown;

            EnsureLayer();

            _group.DOKill();

            if (shown)
            {
                _layer.gameObject.SetActive(true);
                Sync();
                _group.DOFade(1f, fadeDuration);
                return;
            }

            _group
                .DOFade(0f, fadeDuration)
                .OnComplete(() => _layer.gameObject.SetActive(false));
        }

        private void LateUpdate()
        {
            if (!_shown || _layer == null) return;

            Sync();
        }

        private void Sync()
        {
            var cards = _lane.Cards;

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null) continue;

                var quad = QuadAt(i);
                var rect = (RectTransform)card;
                var size = rect.rect.size;

                var raised = card == _lane.RaisedCard;
                var reach = raised ? 0f : Overlap(cards, i, rect.localScale.x);
                var left = i == 0 || raised ? glowWidth : glowWidth + reach;
                var right = i == cards.Count - 1 || raised ? glowWidth : glowWidth + reach;

                quad.rectTransform.sizeDelta =
                    new Vector2(size.x + left + right, size.y + glowWidth * 2f);
                quad.rectTransform.localRotation = rect.localRotation;
                quad.rectTransform.localScale = rect.localScale;
                quad.rectTransform.localPosition = rect.localPosition
                    + rect.localRotation * new Vector3((right - left) * 0.5f * rect.localScale.x, 0f, 0f);

                if (!quad.gameObject.activeSelf)
                    quad.gameObject.SetActive(true);
            }

            for (var i = cards.Count; i < _quads.Count; i++)
                if (_quads[i].gameObject.activeSelf)
                    _quads[i].gameObject.SetActive(false);
        }

        private static float Overlap(IReadOnlyList<Transform> cards, int index, float scale)
        {
            if (cards.Count < 2 || Mathf.Approximately(scale, 0f)) return 0f;

            var neighbour = cards[index > 0 ? index - 1 : index + 1];
            if (neighbour == null) return 0f;

            return Mathf.Abs(neighbour.localPosition.x - cards[index].localPosition.x) / scale;
        }

        private Image QuadAt(int index)
        {
            while (_quads.Count <= index)
                _quads.Add(BuildQuad());

            var quad = _quads[index];

            quad.color = color;
            GlowSprite.SetWidth(quad, glowWidth);

            return quad;
        }

        private Image BuildQuad()
        {
            var image = GlowSprite.Create(_layer, "Glow", glowMaterial);
            image.color = color;

            return image;
        }

        private void EnsureLayer()
        {
            if (_layer != null) return;

            var go = new GameObject("HandGlow", typeof(RectTransform), typeof(CanvasGroup), typeof(HandOutlineLayer));

            _layer = (RectTransform)go.transform;
            _layer.SetParent(transform, false);
            _layer.anchorMin = new Vector2(0.5f, 0.5f);
            _layer.anchorMax = new Vector2(0.5f, 0.5f);
            _layer.pivot = new Vector2(0.5f, 0.5f);
            _layer.sizeDelta = Vector2.zero;
            _layer.anchoredPosition = Vector2.zero;
            _layer.SetAsFirstSibling();

            _group = go.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            go.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_group != null)
                _group.DOKill();
        }
    }
}
