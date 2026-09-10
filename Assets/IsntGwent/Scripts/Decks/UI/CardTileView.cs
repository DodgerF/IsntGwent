using System;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.UI;
using TMPro;
using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Decks.UI
{
    public class CardTileView : MonoBehaviour
    {
        private const float DisabledAlpha = 0.4f;

        public CardView cardView;
        public TextMeshProUGUI badgeText;
        public HoldButton holdButton;
        public CanvasGroup canvasGroup;

        private RectTransform _rt;

        public string CardId { get; private set; }
        public CardDefinition Definition { get; private set; }

        public IObservable<Unit> Clicked => holdButton.Clicked;

        public IObservable<Unit> HoldStarted => holdButton.HoldStarted;
        public IObservable<Unit> HoldEnded => holdButton.HoldEnded;

        private void Awake()
        {
            _rt = (RectTransform)transform;
            FitCard();
        }

        public void Setup(CardDefinition definition, bool interactive = true)
        {
            CardId = definition.Id;
            Definition = definition;

            if (holdButton != null)
                holdButton.enabled = interactive;

            cardView.hoverSfx = !interactive;
            cardView.Setup(definition);

            FitCard();
        }

        public void Setup(CardInstance instance, bool interactive = true)
        {
            CardId = instance.Definition.Id;
            Definition = instance.Definition;

            if (holdButton != null)
                holdButton.enabled = interactive;

            cardView.hoverSfx = !interactive;
            cardView.Setup(instance);

            FitCard();
        }

        public void SetCount(int count)
        {
            if (badgeText == null) return;

            badgeText.gameObject.SetActive(count > 1);
            badgeText.text = "x" + count;
        }

        public void SetRemaining(int remaining)
        {
            if (badgeText != null)
            {
                badgeText.gameObject.SetActive(true);
                badgeText.text = "x" + remaining;
            }

            holdButton.Interactable = remaining > 0;

            if (canvasGroup != null)
                canvasGroup.alpha = remaining > 0 ? 1f : DisabledAlpha;
        }

        private void FitCard()
        {
            if (cardView == null) return;

            if (_rt == null)
                _rt = (RectTransform)transform;

            var size = _rt.rect.size;
            if (size.x <= 0f || size.y <= 0f) return;

            var scale = Mathf.Min(size.x / CardView.NativeSize.x, size.y / CardView.NativeSize.y);

            cardView.SetBaseScale(scale);

            if (badgeText != null)
                badgeText.rectTransform.localScale = Vector3.one * scale;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                FitCard();
        }
    }
}
