using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class DeckSelectionView : MonoBehaviour
    {
        private Image _image;
        private DeckDefinition _deck;
        public TextMeshProUGUI deckName;

        private void Awake()
        {
            _image = GetComponent<Image>();
        }

        private void Start()
        {
            var button = GetComponent<Button>();
            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Debug.Log(_deck.Name);
                })
                .AddTo(this);
        }

        public void Setup(Sprite deckSprite, DeckDefinition deck)
        {
            _image.sprite = deckSprite;
            _deck = deck;
            deckName.text = deck.Name;
        }
    }
}