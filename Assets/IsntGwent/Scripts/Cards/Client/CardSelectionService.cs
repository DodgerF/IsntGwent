using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Cards.Client
{
    public class CardSelectionService : IInitializable, IDisposable
    {
        [Inject] private readonly InputRouter _inputRouter;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardPreviewService _previewService;
        [Inject] private readonly CardResolver _cardResolver;
         
        public readonly Subject<CardDefinition> HighlightRows = new();
        public readonly Subject<Unit> ClearHighlights = new();
        public readonly Subject<(CardInstance Card, RowView Row, List<string> TargetIds)> CardPlayRequested = new();
        public readonly Subject<List<string>> HighlightTargets = new();
        public readonly Subject<string> TargetSelected = new();
        public readonly Subject<string> TargetDeselected = new();
        
        private List<string> _selectedTargets = new();
        private int _requiredTargets;
        
        private enum State { Idle, CardSelected, TargetSelection, TargetThenRow }
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
            
            _inputRouter.BoardCardPressed
                .Subscribe(OnBoardCardClicked)
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
                case State.TargetSelection:
                case State.TargetThenRow:
                    CancelSelection();
                    SelectCard(card);
                    break;
            }
        }

        private void OnRowClicked(RowView row)
        {
            if (_state != State.CardSelected) return;
            
            if (_selectedCard.Instance is UnitInstance unit && unit.UnitDefinition.Row != row.row)
            {
                CancelSelection();
                return;
            }

            CardPlayRequested.OnNext((_selectedCard.Instance, row, new List<string>(_selectedTargets)));
            CancelSelection();
        }

        private void OnEmptyClicked()
        {
            if (_state == State.Idle) return;
            CancelSelection();
        }

        private bool _allowPartial;
        private List<string> _targetPool;
        private void SelectCard(CardView card)
        {
            if (!_matchState.Hand.Contains(card.Instance)) return;
            if (card.mode == CardMode.OnBoard) return;
            if (!_matchState.IsMyTurn.Value) return;
            if (_matchState.IsActionPending.Value) return;
            
            _selectedCard = card;
            _selectedCard.SetSelected(true);
            _previewService.ShowCard.OnNext(card.Instance);
            
            var needsManual = _cardResolver.NeedsManualTargets(card.Instance.Definition, out var count);
            if (needsManual)
            {
                var targetingDef = card.Instance.Definition.Effects
                    .OfType<ManualTargetingDefinition>().FirstOrDefault();
                _allowPartial = targetingDef?.AllowPartial ?? false;
                foreach (var own in _matchState.OwnMeleeRow)
                {
                    Debug.Log(own.Definition.Id);
                }
                
                _targetPool = _cardResolver.GetTargetPool(
                    card.Instance.Definition,
                    _matchState.OwnMeleeRow.Concat(_matchState.OwnRangedRow).Cast<UnitInstance>(),
                    _matchState.EnemyMeleeRow.Concat(_matchState.EnemyRangedRow).Cast<UnitInstance>()
                );
                if (_targetPool.Count == 0 && _allowPartial)
                {
                    if (card.Instance is UnitInstance)
                    {
                        HighlightRows.OnNext(card.Instance.Definition);
                        _state = State.CardSelected;
                        return;
                    }
                    
                    CardPlayRequested.OnNext((card.Instance, null, new List<string>()));
                    CancelSelection();
                    return;
                }
                
                _requiredTargets = count;
                _selectedTargets = new List<string>();
                
                _state = card.Instance is UnitInstance ? State.TargetThenRow : State.TargetSelection;
                
                HighlightTargets.OnNext(_targetPool);
                return;
            }
            
            if (card.Instance is UnitInstance)
            {
                HighlightRows.OnNext(card.Instance.Definition);
                _state = State.CardSelected;
                return;
            }
            
            CardPlayRequested.OnNext((card.Instance, null, new List<string>()));
            CancelSelection();
        }
        
        private void OnBoardCardClicked(CardView target)
        {
            if (_state is not (State.TargetSelection or State.TargetThenRow)) return;
            if (target.Instance is not UnitInstance unit) return;
            if (!_targetPool.Contains(target.Instance.Id.ToString()))
            {
                CancelSelection();
                return;
            }

            var id = unit.Id.ToString();
            if (_selectedTargets.Contains(id))
            {
                _selectedTargets.Remove(id);
                TargetDeselected.OnNext(id);
                return;
            }

            _selectedTargets.Add(id);
            TargetSelected.OnNext(id);

            
            var requiredReached = _selectedTargets.Count >= _requiredTargets;
            var poolExhausted = _allowPartial && _selectedTargets.Count >= _targetPool.Count;

            if (requiredReached || poolExhausted)
                ConfirmTargets();
        }
        
        private void ConfirmTargets()
        {
            if (_state == State.TargetThenRow)
            {
                HighlightRows.OnNext(_selectedCard.Instance.Definition);
                _state = State.CardSelected;
            }
            else
            {
                CardPlayRequested.OnNext((_selectedCard.Instance, null, new List<string>(_selectedTargets)));
                CancelSelection();
            }
        }


        private void CancelSelection()
        {
            if (_selectedCard != null)
                _selectedCard.SetSelected(false);
            
            _previewService.HideCard.OnNext(Unit.Default);
            ClearHighlights.OnNext(Unit.Default);
            HighlightTargets.OnNext(new List<string>());
            _selectedTargets.Clear();
            _selectedCard = null;
            _state = State.Idle;
        }

        public void Dispose() => _disposables.Dispose();
    }
}