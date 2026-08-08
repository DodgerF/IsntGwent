using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Decks.UI;
using IsntGwent.Scripts.Lobby.Client;
using ModestTree;
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

        public DeckStackView stackPrefab;

        [Inject] private CardDatabase _cardDatabase;
        [Inject] private DeckSelectService _deckSelectService;
        [Inject] private DiContainer _container;

        private void Start()
        {
            _deckSelectService.SelectedDeck
                .Subscribe(ViewCards)
                .AddTo(this);
        }

        private void ViewCards(DeckDefinition deck)
        {
            ClearRows();

            if (deck == null || deck.Cards.IsEmpty())
                return;

            foreach (var pair in CountCards(deck))
            {
                if (!_cardDatabase.Cards.TryGetValue(pair.Key, out var cardDefinition)) continue;

                var stack = _container.InstantiatePrefabForComponent<DeckStackView>(stackPrefab);
                stack.Setup(cardDefinition, pair.Value, false);

                AddStackToRow(stack, cardDefinition);
            }
        }

        private static IEnumerable<KeyValuePair<string, int>> CountCards(DeckDefinition deck)
        {
            var counts = new Dictionary<string, int>();
            var order = new List<string>();

            foreach (var cardEntry in deck.Cards)
            {
                if (counts.TryGetValue(cardEntry.CardId, out var count))
                {
                    counts[cardEntry.CardId] = count + cardEntry.Count;
                    continue;
                }

                counts[cardEntry.CardId] = cardEntry.Count;
                order.Add(cardEntry.CardId);
            }

            foreach (var cardId in order)
                yield return new KeyValuePair<string, int>(cardId, counts[cardId]);
        }

        private void AddStackToRow(DeckStackView stack, CardDefinition cardDefinition)
        {
            if (cardDefinition is not UnitDefinition unit)
            {
                spellRow.AddCard(stack.gameObject);
                return;
            }

            switch (unit.Row)
            {
                case RowType.Melee:
                    meleeRow.AddCard(stack.gameObject);
                    break;

                case RowType.Ranged:
                    rangedRow.AddCard(stack.gameObject);
                    break;

                default:
                    spellRow.AddCard(stack.gameObject);
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
