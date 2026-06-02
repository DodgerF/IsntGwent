using IsntGwent.Scripts.Decks.Definitions;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Decks.UI
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class DeckSelectionView : MonoBehaviour
    {
        private Image _image;
        private DeckDefinition _deck;
        public TextMeshProUGUI deckName;
        
        [Inject] private DeckPreviewService _deckPreviewService;

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
                    _deckPreviewService.OnDeckSelected.OnNext(_deck);
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