using System;
using IsntGwent.Scripts.Decks.Definitions;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class DeckSelectionView : MonoBehaviour
    {
        private Image _image;
        private Button _button;

        public TextMeshProUGUI deckName;
        public GameObject incompletePanel;
        public TextMeshProUGUI incompleteText;
        public GameObject selectedFrame;

        public DeckDefinition Deck { get; private set; }

        public IObservable<Unit> Clicked => _button.OnClickAsObservable();

        private void Awake()
        {
            _image = GetComponent<Image>();
            _button = GetComponent<Button>();
        }

        public void Setup(Sprite deckSprite, DeckDefinition deck)
        {
            _image.sprite = deckSprite;
            Deck = deck;
            deckName.text = deck.Name;
        }

        public void SetInteractable(bool value)
        {
            _button.interactable = value;
        }

        public void SetSelected(bool value)
        {
            if (selectedFrame != null)
                selectedFrame.SetActive(value);
        }

        public void SetIncomplete(string reason)
        {
            var hasReason = !string.IsNullOrEmpty(reason);

            if (incompleteText != null)
                incompleteText.text = reason;

            if (incompletePanel != null)
                incompletePanel.SetActive(hasReason);
        }
    }
}
