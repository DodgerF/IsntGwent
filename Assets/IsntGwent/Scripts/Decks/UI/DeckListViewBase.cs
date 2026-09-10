using System.Collections.Generic;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.UI.Views;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Decks.UI
{
    public abstract class DeckListViewBase : MonoBehaviour
    {
        public Transform parent;
        public DeckSelectionView deckViewPrefab;
        public NewDeckTileView newDeckTilePrefab;

        [Inject] private readonly DeckDatabase _deckDatabase;
        [Inject] private readonly CardDatabase _cardDatabase;
        [Inject] private readonly DeckRulesProvider _rules;
        [Inject] private readonly DiContainer _container;

        [Inject] protected readonly DeckValidator Validator;
        [Inject] protected readonly UserDeckStore UserDecks;

        private readonly Dictionary<string, DeckSelectionView> _views = new();

        private NewDeckTileView _newDeckTile;
        private string _selectedId;

        protected virtual void Start()
        {
            Observable.CombineLatest(_deckDatabase.OnLoaded, _cardDatabase.OnLoaded, _rules.OnLoaded)
                .Where(values => values[0] && values[1] && values[2])
                .Take(1)
                .Subscribe(_ => Populate())
                .AddTo(this);
        }

        protected abstract void OnDeckClicked(DeckDefinition deck, bool isBuiltIn, DeckSelectionView view);

        protected abstract void OnNewDeckClicked();

        protected virtual void ConfigureView(DeckSelectionView view, DeckDefinition deck, IReadOnlyList<DeckViolation> violations)
        {
        }

        protected virtual void OnPopulated()
        {
        }

        protected DeckSelectionView ViewOf(string deckId)
        {
            if (string.IsNullOrEmpty(deckId)) return null;

            return _views.TryGetValue(deckId, out var view) ? view : null;
        }

        protected void SetSelected(string deckId)
        {
            _selectedId = deckId;

            foreach (var pair in _views)
                pair.Value.SetSelected(pair.Key == deckId);
        }

        private void Populate()
        {
            foreach (var deck in _deckDatabase.GetAll())
                CreateView(deck);

            foreach (var deck in UserDecks.Decks)
                CreateView(deck);

            CreateNewDeckTile();

            UserDecks.Decks.ObserveAdd()
                .Subscribe(added => CreateView(added.Value))
                .AddTo(this);

            UserDecks.Decks.ObserveReplace()
                .Subscribe(replaced =>
                {
                    RemoveView(replaced.OldValue);
                    CreateView(replaced.NewValue);
                })
                .AddTo(this);

            UserDecks.Decks.ObserveRemove()
                .Subscribe(removed => RemoveView(removed.Value))
                .AddTo(this);

            OnPopulated();
        }

        private void CreateView(DeckDefinition deck)
        {
            if (deck == null || string.IsNullOrEmpty(deck.Id)) return;
            if (_views.ContainsKey(deck.Id)) return;

            var isBuiltIn = _deckDatabase.Contains(deck.Id);

            var view = _container.InstantiatePrefabForComponent<DeckSelectionView>(deckViewPrefab, parent);
            view.Setup(FirstCardSprite(deck), deck);

            var violations = Validator.Validate(deck);
            view.SetIncomplete(DeckViolationText.DescribeFirst(violations));
            view.SetSelected(deck.Id == _selectedId);
            ConfigureView(view, deck, violations);

            view.Clicked
                .Subscribe(_ => OnDeckClicked(deck, isBuiltIn, view))
                .AddTo(view);

            _views.Add(deck.Id, view);

            if (_newDeckTile != null)
                _newDeckTile.transform.SetAsLastSibling();
        }

        private void RemoveView(DeckDefinition deck)
        {
            if (deck == null || string.IsNullOrEmpty(deck.Id)) return;
            if (!_views.Remove(deck.Id, out var view)) return;

            Destroy(view.gameObject);
        }

        private void CreateNewDeckTile()
        {
            if (newDeckTilePrefab == null) return;

            _newDeckTile = _container.InstantiatePrefabForComponent<NewDeckTileView>(newDeckTilePrefab, parent);
            _newDeckTile.transform.SetAsLastSibling();

            _newDeckTile.Clicked
                .Subscribe(_ => OnNewDeckClicked())
                .AddTo(_newDeckTile);
        }

        private Sprite FirstCardSprite(DeckDefinition deck)
        {
            if (deck.Cards == null) return null;

            foreach (var entry in deck.Cards)
            {
                if (entry == null || string.IsNullOrEmpty(entry.CardId)) continue;
                if (!_cardDatabase.Cards.TryGetValue(entry.CardId, out var card)) continue;

                return Resources.Load<Sprite>("Sprites/Cards/" + card.ImageName);
            }

            return null;
        }
    }
}
