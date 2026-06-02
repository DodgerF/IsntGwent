using System.Collections;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Decks.Definitions;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    public class CardsViewLoader : MonoBehaviour
    {
        public RowView meleeRow;
        public RowView rangedRow;
        public RowView spellRow;
       
        public CardView cardViewPrefab;
        
        [Inject] private CardDatabase _cardDatabase;
        [Inject] private DeckPreviewService _deckPreviewService;
        [Inject] private DiContainer _container;

        private void Start()
        {
            _deckPreviewService.OnDeckSelected
                .Subscribe(deck =>
                {
                    StartCoroutine(ViewCards(deck));
                })
                .AddTo(this);
        }

        private IEnumerator ViewCards(DeckDefinition deck)
        {
            ClearRows();
            
            yield return null; 

            foreach (var cardEntry in deck.Cards)
            {
                var cardDefinition = _cardDatabase.Get(cardEntry.CardId);

                for (var i = 0; i < cardEntry.Count; i++)
                {
                    var cardView = _container.InstantiatePrefabForComponent<CardView>(cardViewPrefab);
                    cardView.Setup(cardDefinition);

                    AddCardToRow(cardView, cardDefinition);
                }
            }
        }

        private void AddCardToRow(CardView cardView, CardDefinition cardDefinition)
        {
            if (cardDefinition is not UnitDefinition unit)
            {
                spellRow.AddCard(cardView.gameObject);
                return;
            }

            switch (unit.Row)
            {
                case RowType.Melee:
                    meleeRow.AddCard(cardView.gameObject);
                    break;

                case RowType.Ranged:
                    rangedRow.AddCard(cardView.gameObject);
                    break;
            }
        }

        private void ClearRows()
        {
            meleeRow.ClearCards();
            rangedRow.ClearCards();
            spellRow.ClearCards();
        }
    }
}