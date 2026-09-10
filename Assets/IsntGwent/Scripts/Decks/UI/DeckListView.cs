using IsntGwent.Scripts.Localization;
using System;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Lobby.UI.Views;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Decks.UI
{
    public class DeckListView : DeckListViewBase
    {
        private const float DrawStep = 0.03f;
        private const int DrawMaxSounds = 8;

        private const string UnsavedMessage = "Unsaved changes will be lost. Continue?";

        public Button deleteButton;

        [Inject] private readonly DeckDraft _draft;
        [Inject] private readonly AudioService _audio;
        [Inject] private readonly ConfirmWindow _confirm;
        [Inject] private readonly DeckSelectService _deckSelect;
        [InjectOptional] private readonly DeckContentView _content;

        private readonly SerialDisposable _drawSounds = new();

        private string _openListId;
        private string _openDraftId;

        protected override void Start()
        {
            base.Start();

            _drawSounds.AddTo(this);

            if (deleteButton != null)
            {
                deleteButton.OnClickAsObservable()
                    .Subscribe(_ => _confirm.Ask(DeleteMessage(), DeleteCurrent))
                    .AddTo(this);
            }

            _draft.Changed
                .Subscribe(_ => RefreshCurrent())
                .AddTo(this);

            UserDecks.Decks
                .ObserveCountChanged()
                .Subscribe(_ => RefreshCurrent())
                .AddTo(this);

            RefreshCurrent();
        }

        protected override void OnPopulated()
        {
            var deck = _deckSelect.ConsumeEditTarget(out var isBuiltIn);

            if (deck == null)
            {
                _draft.NewDeck();
                return;
            }

            Open(deck, isBuiltIn, ViewOf(deck.Id));
        }

        protected override void OnDeckClicked(DeckDefinition deck, bool isBuiltIn, DeckSelectionView view)
        {
            AskIfDirty(() => Open(deck, isBuiltIn, view));
        }

        protected override void OnNewDeckClicked()
        {
            AskIfDirty(() => _draft.NewDeck());
        }

        private void AskIfDirty(Action action)
        {
            if (_draft.IsDirty)
                _confirm.Ask(Loc.T(UnsavedMessage), action);
            else
                action();
        }

        private void Open(DeckDefinition deck, bool isBuiltIn, DeckSelectionView view)
        {
            _content?.SpawnFrom(view != null ? (RectTransform)view.transform : null);
            _draft.LoadFrom(deck, isBuiltIn);
            _content?.SpawnFrom(null);

            _openListId = deck.Id;
            _openDraftId = _draft.Id;

            RefreshCurrent();
            PlayDrawSounds(_draft.Cards.Count);
        }

        private void DeleteCurrent()
        {
            UserDecks.Delete(_draft.Id);
            _draft.NewDeck();
        }

        private string DeleteMessage()
        {
            return Loc.F("Delete deck \"{0}\"?", _draft.Name.Value);
        }

        private void RefreshCurrent()
        {
            if (_draft.Id != _openDraftId)
            {
                _openListId = null;
                _openDraftId = null;
            }

            SetSelected(_openListId);

            if (deleteButton != null)
                deleteButton.interactable = UserDecks.Contains(_draft.Id);
        }

        private void PlayDrawSounds(int count)
        {
            if (count <= 0)
            {
                _drawSounds.Disposable = null;
                return;
            }

            _drawSounds.Disposable = Observable
                .Timer(TimeSpan.Zero, TimeSpan.FromSeconds(DrawStep))
                .Take(Mathf.Min(count, DrawMaxSounds))
                .Subscribe(_ => _audio.Play("card_draw_menu"));
        }
    }
}
