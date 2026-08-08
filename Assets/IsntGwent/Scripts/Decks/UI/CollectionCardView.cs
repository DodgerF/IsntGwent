using System;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.UI;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Decks.UI
{
    public class CollectionCardView : MonoBehaviour
    {
        private const float DisabledAlpha = 0.4f;

        public Image cardImage;
        public TextMeshProUGUI powerText;
        public TextMeshProUGUI remainingText;
        public GameObject meleeIcon;
        public GameObject rangedIcon;
        public HoldButton holdButton;
        public CanvasGroup canvasGroup;

        public CardDefinition Definition { get; private set; }

        public IObservable<Unit> Clicked => holdButton.Clicked;
        public IObservable<Unit> HoldStarted => holdButton.HoldStarted;
        public IObservable<Unit> HoldEnded => holdButton.HoldEnded;

        public void Setup(CardDefinition definition)
        {
            Definition = definition;

            cardImage.sprite = Resources.Load<Sprite>("Sprites/Cards/" + definition.ImageName);

            if (definition is UnitDefinition unit)
            {
                if (powerText != null)
                {
                    powerText.gameObject.SetActive(true);
                    powerText.text = unit.Power.ToString();
                }

                if (meleeIcon != null) meleeIcon.SetActive(unit.Row == RowType.Melee);
                if (rangedIcon != null) rangedIcon.SetActive(unit.Row == RowType.Ranged);
            }
            else
            {
                if (powerText != null) powerText.gameObject.SetActive(false);
                if (meleeIcon != null) meleeIcon.SetActive(false);
                if (rangedIcon != null) rangedIcon.SetActive(false);
            }
        }

        public void SetRemaining(int remaining)
        {
            if (remainingText != null)
                remainingText.text = remaining.ToString();

            holdButton.Interactable = remaining > 0;

            if (canvasGroup != null)
                canvasGroup.alpha = remaining > 0 ? 1f : DisabledAlpha;
        }
    }
}
