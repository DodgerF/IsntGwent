using System;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Cards.Services
{
    public class CardSelectionService : IInitializable, IDisposable
    {
        [Inject] private readonly InputRouter _inputRouter;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardPreviewService _previewService;
         
        public readonly Subject<CardDefinition> HighlightRows = new();
        public readonly Subject<Unit> ClearHighlights = new();
        public readonly Subject<(CardInstance Card, RowView Row)> CardPlayRequested = new();
        
        private enum State { Idle, CardSelected, TargetSelection }
        private State _state = State.Idle;
        private CardView _selectedCard;
        
        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            _inputRouter.CardPressed
                .Subscribe(OnCardClicked)
                .AddTo(_disposables);

            _inputRouter.RowPressed
                .Subscribe(OnRowClicked)
                .AddTo(_disposables);

            _inputRouter.EmptyPressed
                .Subscribe(_ => OnEmptyClicked())
                .AddTo(_disposables);
        }

        private void OnCardClicked(CardView card)
        {
            switch (_state)
            {
                case State.Idle:
                    SelectCard(card);
                    break;
                case State.CardSelected:
                    CancelSelection();
                    SelectCard(card);
                    break;
            }
        }

        private void OnRowClicked(RowView row)
        {
            if (_state != State.CardSelected) return;
            CardPlayRequested.OnNext((_selectedCard.Instance, row));
            CancelSelection();
        }

        private void OnEmptyClicked()
        {
            if (_state == State.Idle) return;
            CancelSelection();
        }

        private void SelectCard(CardView card)
        {
            if (card.mode == CardMode.OnBoard) return;
            if (!_matchState.IsMyTurn.Value) return;
            
            _selectedCard = card;
            _selectedCard.SetSelected(true);
            _previewService.ShowCard.OnNext(card.Instance);
            HighlightRows.OnNext(card.Instance.Definition);
            _state = State.CardSelected;
        }

        private void CancelSelection()
        {
            
            _selectedCard.SetSelected(false);
            _previewService.HideCard.OnNext(Unit.Default);
            
                
            ClearHighlights.OnNext(Unit.Default);
            _selectedCard = null;
            _state = State.Idle;
        }

        public void Dispose() => _disposables.Dispose();
    }
}