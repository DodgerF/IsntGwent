using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    public class DecksViewLoader : MonoBehaviour
    {
        public Transform parent;
        public DeckSelectionView deckViewPrefab;
        
        [Inject] private DeckDatabase _deckDatabase;
        [Inject] private CardDatabase _cardDatabase;
        [Inject] private DiContainer _container;

        private void Start()
        {
            Observable.CombineLatest(_deckDatabase.OnLoaded, _cardDatabase.OnLoaded)
                .Where(values => values[0] && values[1])
                .Take(1)
                .Subscribe(_ => Populate())
                .AddTo(this);
        }
        
        private void Populate()
        {
            foreach (var deckDefinition in _deckDatabase.GetAll())
            {
                var instance = _container.InstantiatePrefabForComponent<DeckSelectionView>(deckViewPrefab, parent);
                var firstCardId = deckDefinition.Cards[0].CardId;
                var spritePath = "Sprites/Cards/" + _cardDatabase.Get(firstCardId).ImageName;

                instance.Setup(Resources.Load<Sprite>(spritePath), deckDefinition);
            }
        }
    }
}