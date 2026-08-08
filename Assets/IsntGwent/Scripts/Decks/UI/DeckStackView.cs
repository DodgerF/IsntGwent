using System;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.UI;
using TMPro;
using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Decks.UI
{
    public class DeckStackView : MonoBehaviour
    {
        public CardView cardView;
        public TextMeshProUGUI countText;
        public HoldButton holdButton;

        public string CardId { get; private set; }
        public CardDefinition Definition { get; private set; }

        public IObservable<Unit> Clicked => holdButton.Clicked;
        public IObservable<Unit> HoldStarted => holdButton.HoldStarted;
        public IObservable<Unit> HoldEnded => holdButton.HoldEnded;

        public void Setup(CardDefinition definition, int count, bool interactive = true)
        {
            CardId = definition.Id;
            Definition = definition;

            if (holdButton != null)
                holdButton.enabled = interactive;

            cardView.hoverSfx = !interactive;
            cardView.Setup(CardFactory.Create(definition));
            SetCount(count);
        }

        public void SetCount(int count)
        {
            if (countText == null) return;

            countText.gameObject.SetActive(count > 1);
            countText.text = "x" + count;
        }
    }
}
