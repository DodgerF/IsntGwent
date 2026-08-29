using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Decks.UI
{
    public class CollectionView : MonoBehaviour
    {
        public Transform grid;
        public CardTileView cardPrefab;

        [Inject] private readonly CardDatabase _cardDatabase;
        [Inject] private readonly DeckRulesProvider _rules;
        [Inject] private readonly DeckDraft _draft;
        [Inject] private readonly AudioService _audio;
        [Inject] private readonly CardTooltipView _tooltip;
        [Inject] private readonly DiContainer _container;

        private readonly Dictionary<string, CardTileView> _views = new();
        private readonly HashSet<string> _incoming = new();

        private void Start()
        {
            Observable.CombineLatest(_cardDatabase.OnLoaded, _rules.OnLoaded)
                .Where(values => values[0] && values[1])
                .Take(1)
                .Subscribe(_ => Populate())
                .AddTo(this);
        }

        private void Populate()
        {
            foreach (var definition in Sorted())
            {
                var view = _container.InstantiatePrefabForComponent<CardTileView>(cardPrefab, grid);
                view.Setup(definition);
                view.SetRemaining(_draft.RemainingOf(definition.Id));

                var cardId = definition.Id;
                view.Clicked
                    .Subscribe(_ => Add(cardId))
                    .AddTo(view);

                view.HoldStarted
                    .Subscribe(_ => _tooltip.Show(view.Definition, (RectTransform)view.transform))
                    .AddTo(view);

                view.HoldEnded
                    .Subscribe(_ => _tooltip.Hide())
                    .AddTo(view);

                _views[cardId] = view;
            }

            _draft.Changed
                .Subscribe(_ => RefreshAll())
                .AddTo(this);

            RefreshAll();
        }

        public RectTransform TileOf(string cardId)
            => _views.TryGetValue(cardId, out var view) ? (RectTransform)view.transform : null;

        public void HoldIncoming(string cardId) => _incoming.Add(cardId);

        public void ReleaseIncoming(string cardId)
        {
            if (!_incoming.Remove(cardId)) return;

            if (_views.TryGetValue(cardId, out var view))
                view.SetRemaining(_draft.RemainingOf(cardId));
        }

        private void Add(string cardId)
        {
            if (_draft.TryAdd(cardId))
                _audio.Play("deck_card_add");
        }

        private void RefreshAll()
        {
            foreach (var pair in _views)
            {
                if (_incoming.Contains(pair.Key)) continue;

                pair.Value.SetRemaining(_draft.RemainingOf(pair.Key));
            }
        }

        private IEnumerable<CardDefinition> Sorted()
        {
            return _cardDatabase.Cards.Values
                .Where(card => !card.IsToken)
                .OrderBy(card => card, DeckCardOrder.Comparer);
        }
    }
}
