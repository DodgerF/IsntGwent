using System.Collections.Generic;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Decks.UI;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    public class DecksViewLoader : DeckListViewBase
    {
        [Inject] private readonly DeckSelectService _deckSelect;
        [Inject] private readonly SceneService _scenes;
        [InjectOptional] private readonly CardsViewLoader _cards;

        protected override void Start()
        {
            base.Start();

            _deckSelect.SelectedDeck
                .Subscribe(deck => SetSelected(deck?.Id))
                .AddTo(this);
        }

        protected override void OnDeckClicked(DeckDefinition deck, bool isBuiltIn, DeckSelectionView view)
        {
            _cards?.SpawnFrom(view != null ? (RectTransform)view.transform : null);
            _deckSelect.SelectedDeck.Value = deck;
            _cards?.SpawnFrom(null);
        }

        protected override void OnNewDeckClicked()
        {
            _deckSelect.RequestNewDeck();
            _scenes.LoadDeckBuilder();
        }

        protected override void ConfigureView(DeckSelectionView view, DeckDefinition deck, IReadOnlyList<DeckViolation> violations)
        {
            view.SetInteractable(violations.Count == 0);
        }
    }
}
