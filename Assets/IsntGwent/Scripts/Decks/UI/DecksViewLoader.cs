using IsntGwent.Scripts.Cards;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Decks.UI
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