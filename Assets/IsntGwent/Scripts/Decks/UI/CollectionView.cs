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
        public CollectionCardView cardPrefab;

        [Inject] private readonly CardDatabase _cardDatabase;
        [Inject] private readonly DeckRulesProvider _rules;
        [Inject] private readonly DeckDraft _draft;
        [Inject] private readonly AudioService _audio;
        [Inject] private readonly CardTooltipView _tooltip;
        [Inject] private readonly DiContainer _container;

        private readonly Dictionary<string, CollectionCardView> _views = new();

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
                var view = _container.InstantiatePrefabForComponent<CollectionCardView>(cardPrefab, grid);
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

        private void Add(string cardId)
        {
            if (_draft.TryAdd(cardId))
                _audio.Play("deck_card_add");
        }

        private void RefreshAll()
        {
            foreach (var pair in _views)
                pair.Value.SetRemaining(_draft.RemainingOf(pair.Key));
        }

        private IEnumerable<CardDefinition> Sorted()
        {
            return _cardDatabase.Cards.Values
                .OrderBy(SortGroup)
                .ThenByDescending(SortPower)
                .ThenBy(card => card.Id);
        }

        private static int SortGroup(CardDefinition card)
        {
            if (card is not UnitDefinition unit) return 2;

            return unit.Row == RowType.Melee ? 0 : 1;
        }

        private static int SortPower(CardDefinition card)
        {
            return card is UnitDefinition unit ? unit.Power : 0;
        }
    }
}
