using System.Collections.Generic;
using System.Linq;
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
        public CardLaneView[] lanes;

        public CardTileView stackPrefab;

        [Inject] private CardDatabase _cardDatabase;
        [Inject] private DeckSelectService _deckSelectService;
        [Inject] private DiContainer _container;

        private RectTransform _spawnOrigin;

        public void SpawnFrom(RectTransform origin)
        {
            _spawnOrigin = origin;
        }

        private void OnRectTransformDimensionsChange()
        {
            DeckLaneLayout.Apply((RectTransform)transform, lanes);
        }

        private void Start()
        {
            DeckLaneLayout.Apply((RectTransform)transform, lanes);

            _deckSelectService.SelectedDeck
                .Subscribe(ViewCards)
                .AddTo(this);
        }

        private void ViewCards(DeckDefinition deck)
        {
            ClearLanes();

            if (deck == null || deck.Cards.IsEmpty()) return;
            if (lanes == null || lanes.Length == 0) return;

            var ordered = CountCards(deck)
                .Select(pair => _cardDatabase.Cards.TryGetValue(pair.Key, out var definition)
                    ? new KeyValuePair<CardDefinition, int>(definition, pair.Value)
                    : default)
                .Where(pair => pair.Key != null)
                .OrderBy(pair => pair.Key, DeckCardOrder.Comparer)
                .ToList();

            for (var i = 0; i < ordered.Count; i++)
            {
                var stack = _container.InstantiatePrefabForComponent<CardTileView>(stackPrefab);
                stack.Setup(ordered[i].Key, false);
                stack.SetCount(ordered[i].Value);

                if (_spawnOrigin != null)
                {
                    stack.transform.SetParent(_spawnOrigin, false);
                    stack.transform.position = _spawnOrigin.position;
                }

                lanes[DeckCardOrder.LaneOf(i, DeckLaneLayout.LaneCapacity, lanes.Length)].AddCard(stack.gameObject);
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

        private void ClearLanes()
        {
            if (lanes == null) return;

            foreach (var lane in lanes)
                lane.ClearCards();
        }
    }
}
