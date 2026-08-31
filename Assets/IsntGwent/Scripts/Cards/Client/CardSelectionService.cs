using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Cards.Client
{
    public class CardSelectionService : IInitializable, IDisposable
    {
        [Inject] private readonly InputRouter _inputRouter;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardPreviewService _previewService;
        [Inject] private readonly CardResolver _cardResolver;
        [Inject] private readonly BoardTargetQuery _targetQuery;
        [Inject] private readonly PlayPreviewQuery _playPreview;
        [Inject] private readonly ClientConditionQuery _conditions;
        [Inject] private readonly CardViewRegistry _views;

        public enum RowScope { Own, Enemy, Any }

        public readonly Subject<RowScope> HighlightRowTargets = new();
        public readonly Subject<BoardRowView> HighlightRowHover = new();
        public readonly Subject<bool> HighlightFreeSlots = new();
        public readonly Subject<IReadOnlyList<BoardCell>> HighlightCells = new();
        public readonly Subject<BoardCell?> HighlightPlacementCell = new();
        public readonly Subject<Unit> ClearHighlights = new();
        public readonly Subject<Unit> PlacementDenied = new();
        public readonly Subject<(CardInstance Card, CardLaneView Row, int SlotIndex, List<string> TargetIds)>
            CardPlayRequested = new();
        public readonly Subject<List<string>> HighlightTargets = new();
        public readonly Subject<PlayPreviewQuery.Prediction> HighlightPredicted = new();
        public readonly Subject<string> TargetSelected = new();
        public readonly Subject<string> TargetDeselected = new();
        public readonly Subject<List<string>> AimTargetsChosen = new();

        public readonly Subject<CardView> DragBegan = new();
        public readonly Subject<Unit> SlotHovered = new();
        public readonly Subject<(CardInstance Card, BoardRowView Row, int SlotIndex)> PlacementStaged = new();
        public readonly Subject<Unit> PlacementCancelled = new();

        private List<string> _selectedTargets = new();
        private int _requiredTargets;

        private enum State { Idle, Picked, TargetSelection, Dragging, AimAfterPlacement, ServerAim }
        private enum Plan { PlaceUnit, AimUnit, ManualTargets, RowChoice, Confirm }

        public readonly ReactiveProperty<bool> IsChoosing = new(false);

        private State _stateValue = State.Idle;

        private State _state
        {
            get => _stateValue;
            set
            {
                if (_stateValue == value) return;

                _stateValue = value;
                IsChoosing.Value = value != State.Idle;
                _inputRouter.TrackHover = value == State.Picked;
            }
        }
        private Plan _plan;

        public bool IsSelectingTargets => _state is not (State.Idle or State.Dragging);

        public CardInstance SelectedCard => _selectedCard != null ? _selectedCard.Instance : null;

        private CardView _selectedCard;
        private AimedTargetingDefinition _aimDefinition;
        private List<AimedTargetingDefinition> _aimSequence = new();
        private int _aimIndex;
        private List<string> _confirmedTargets = new();
        private bool _traitor;
        private BoardRowView _stagedRow;
        private int _stagedSlot = -1;
        private bool _cardDetached;
        private RowScope _rowScope;
        private BoardCell _hoveredAnchor;
        private bool _hoveredAnchorValid;
        private BoardRowView _hoveredRow;

        private bool _allowPartial;
        private List<string> _targetPool;

        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            _inputRouter.CardPressed
                .Subscribe(OnCardClicked)
                .AddTo(_disposables);

            _inputRouter.CardInspected
                .Subscribe(_ => CancelIfBusy())
                .AddTo(_disposables);

            _inputRouter.RowPressed
                .Subscribe(OnRowClicked)
                .AddTo(_disposables);

            _inputRouter.SlotPressed
                .Subscribe(OnSlotClicked)
                .AddTo(_disposables);

            _inputRouter.EmptyPressed
                .Subscribe(_ => OnEmptyClicked())
                .AddTo(_disposables);

            _inputRouter.BoardCardPressed
                .Subscribe(OnBoardCardClicked)
                .AddTo(_disposables);

            _inputRouter.DragCandidate
                .Subscribe(OnDragCandidate)
                .AddTo(_disposables);

            _inputRouter.DragMoved
                .Subscribe(OnDragMoved)
                .AddTo(_disposables);

            _inputRouter.PointerMoved
                .Subscribe(OnPointerMoved)
                .AddTo(_disposables);

            _inputRouter.DragEnded
                .Subscribe(OnDragEnded)
                .AddTo(_disposables);

            Observable.Merge(
                    _matchState.ActionTimedOut.AsUnitObservable(),
                    _matchState.PendingPlays.ObserveCountChanged().AsUnitObservable(),
                    _matchState.IsPendingMine.AsUnitObservable(),
                    _matchState.IsActionPending.AsUnitObservable(),
                    _matchState.IsMyTurn.AsUnitObservable())
                .ThrottleFrame(1)
                .Subscribe(_ => TryArmPendingPlay())
                .AddTo(_disposables);
        }

        private void CancelIfBusy()
        {
            if (_state is State.Idle or State.Dragging) return;

            CancelSelection();
        }

        private void OnEmptyClicked() => CancelIfBusy();

        public void TryArmPendingPlay()
        {
            if (_state != State.Idle) return;
            if (!_matchState.IsPendingMine.Value) return;
            if (_matchState.PendingPlays.Count == 0) return;

            var view = _views.Get(_matchState.PendingPlays[0].Id);
            if (view == null) return;

            Pick(view);

            if (_state == State.Picked && _plan == Plan.Confirm)
                ConfirmPlay();
        }

        private void Pick(CardView card)
        {
            if (!CanSelect(card)) return;

            _selectedCard = card;
            _plan = BuildPlan(card.Instance.Definition);
            _selectedTargets = new List<string>();
            _cardDetached = false;
            _state = State.Picked;

            _previewService.Hide();
            card.SetSelected(true);

            HighlightForPlan();
        }

        private void HighlightForPlan()
        {
            switch (_plan)
            {
                case Plan.AimUnit:
                case Plan.PlaceUnit:
                    HighlightFreeSlots.OnNext(!_traitor);
                    break;
                case Plan.ManualTargets:
                    HighlightTargets.OnNext(_targetPool);
                    break;
                case Plan.RowChoice:
                    HighlightRowTargets.OnNext(_rowScope);
                    break;
                default:
                    HighlightRowTargets.OnNext(RowScope.Any);
                    break;
            }
        }

        private bool CanSelect(CardView card)
        {
            if (_matchState.IsRedrawPhase.Value) return false;
            if (_matchState.IsPileWindowOpen.Value) return false;
            if (card.Instance == null) return false;
            if (_views.Get(card.Instance.Id) != card) return false;
            if (!IsPlayableNow(card.Instance)) return false;
            if (card.mode == CardMode.OnBoard) return false;
            if (!_matchState.IsMyTurn.Value) return false;
            if (_matchState.IsActionPending.Value) return false;
            if (_matchState.IsMatchPaused.Value) return false;

            return true;
        }

        private bool IsPlayableNow(CardInstance card)
        {
            if (_matchState.IsPendingMine.Value && _matchState.PendingPlays.Count > 0)
                return card == _matchState.PendingPlays[0];

            return _matchState.Hand.Contains(card);
        }

        private Plan BuildPlan(CardDefinition definition)
        {
            _aimDefinition = null;
            _aimSequence = new List<AimedTargetingDefinition>();
            _aimIndex = 0;
            _confirmedTargets = new List<string>();
            _targetPool = null;
            _allowPartial = false;
            _requiredTargets = 0;
            _rowScope = RowScope.Any;
            _traitor = definition is UnitDefinition { Traitor: true };

            var isUnit = definition is UnitDefinition;

            if (isUnit)
            {
                _aimSequence = _cardResolver.AimSequence(definition, _conditions.IsMet).Take(1).ToList();

                if (_aimSequence.Count > 0)
                {
                    _aimDefinition = _aimSequence[0];
                    _allowPartial = _aimDefinition.AllowPartial;
                    _requiredTargets = _aimDefinition.Count;
                    return Plan.AimUnit;
                }
            }

            if (_cardResolver.NeedsManualTargets(definition, out var count, _conditions.IsMet))
            {
                var targetingDef = definition.Effects.OfType<ManualTargetingDefinition>()
                    .FirstOrDefault(_conditions.IsMet);
                _allowPartial = targetingDef?.AllowPartial ?? false;
                _requiredTargets = count;

                _targetPool = _cardResolver.GetTargetPool(
                    definition,
                    _matchState.OwnMeleeRow.Units.Concat(_matchState.OwnRangedRow.Units).Cast<UnitInstance>(),
                    _matchState.EnemyMeleeRow.Units.Concat(_matchState.EnemyRangedRow.Units).Cast<UnitInstance>(),
                    _conditions.IsMet
                );

                if (_targetPool.Count > 0 || !_allowPartial)
                    return Plan.ManualTargets;

                _targetPool = null;
            }

            if (isUnit) return Plan.PlaceUnit;

            if (!_cardResolver.NeedsRowChoice(definition)) return Plan.Confirm;

            _rowScope = _cardResolver.TargetsAnyRow(definition) ? RowScope.Enemy : RowScope.Own;
            return Plan.RowChoice;
        }

        private void ConfirmPlay()
        {
            CardPlayRequested.OnNext((_selectedCard.Instance, null, -1, new List<string>()));
            FinishSelection();
        }

        public void BeginServerAim(IReadOnlyList<string> pool)
        {
            ResetSelection();

            if (pool == null || pool.Count == 0) return;

            _targetPool = new List<string>(pool);
            _state = State.ServerAim;

            HighlightTargets.OnNext(_targetPool);
        }

        private bool TryServerAim(CardView card)
        {
            if (_state != State.ServerAim) return false;

            var id = card?.Instance?.Id.ToString();

            if (id != null && _targetPool != null && _targetPool.Contains(id))
            {
                ResetSelection();
                AimTargetsChosen.OnNext(new List<string> { id });
            }

            return true;
        }

        private void OnCardClicked(CardView card)
        {
            if (TryServerAim(card)) return;
            if (_state == State.Dragging) return;

            if (_state != State.Idle)
            {
                var samePick = _state == State.Picked && _selectedCard == card;

                CancelSelection();

                if (samePick) return;
            }

            Pick(card);
        }

        private void OnSlotClicked(SlotView slot)
        {
            if (_state != State.Picked)
            {
                CancelIfBusy();
                return;
            }

            switch (_plan)
            {
                case Plan.PlaceUnit:
                case Plan.AimUnit:
                    PlaceOnSlot(slot);
                    return;
                case Plan.RowChoice:
                    PlayOnRow(slot.Row);
                    return;
                case Plan.Confirm:
                    PlayAnywhere(slot.Row);
                    return;
                default:
                    CancelSelection();
                    return;
            }
        }

        private void OnRowClicked(CardLaneView lane)
        {
            if (_state != State.Picked)
            {
                CancelIfBusy();
                return;
            }

            switch (_plan)
            {
                case Plan.RowChoice:
                    PlayOnRow(lane);
                    return;
                case Plan.Confirm:
                    PlayAnywhere(lane);
                    return;
                default:
                    CancelSelection();
                    return;
            }
        }

        private void PlaceOnSlot(SlotView slot)
        {
            if (!IsPlacementRow(slot.Row))
            {
                CancelSelection();
                return;
            }

            if (!slot.Row.IsSlotFree(slot.Index))
            {
                PlacementDenied.OnNext(Unit.Default);
                CancelSelection();
                return;
            }

            StagePlacement(slot.Row, slot.Index);
        }

        private void PlayOnRow(CardLaneView lane)
        {
            if (lane == null || !IsChoosableRow(lane))
            {
                CancelSelection();
                return;
            }

            CardPlayRequested.OnNext((_selectedCard.Instance, lane, -1, new List<string>()));
            FinishSelection();
        }

        private void PlayAnywhere(CardLaneView lane)
        {
            if (lane is not BoardRowView)
            {
                CancelSelection();
                return;
            }

            ConfirmPlay();
        }

        private void OnBoardCardClicked(CardView target)
        {
            if (TryServerAim(target)) return;

            if (_state == State.Picked)
            {
                switch (_plan)
                {
                    case Plan.ManualTargets:
                        _state = State.TargetSelection;
                        break;
                    case Plan.Confirm:
                        ConfirmPlay();
                        return;
                    default:
                        CancelSelection();
                        return;
                }
            }

            if (_state is not (State.TargetSelection or State.AimAfterPlacement)) return;
            if (target.Instance is not UnitInstance unit) return;
            if (_targetPool == null || !_targetPool.Contains(target.Instance.Id.ToString()))
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
            if (_state == State.AimAfterPlacement)
            {
                _confirmedTargets.AddRange(_selectedTargets);
                _aimIndex++;
                BeginAim();
                return;
            }

            CardPlayRequested.OnNext((_selectedCard.Instance, null, -1, new List<string>(_selectedTargets)));
            FinishSelection();
        }

        private void StagePlacement(BoardRowView row, int slotIndex)
        {
            _stagedRow = row;
            _stagedSlot = slotIndex;
            _cardDetached = true;

            ClearHighlights.OnNext(Unit.Default);
            _previewService.Hide();
            _selectedCard.SetSelected(false);
            PlacementStaged.OnNext((_selectedCard.Instance, row, slotIndex));

            _confirmedTargets = new List<string>();
            _aimIndex = 0;

            BeginAim();
        }

        private void BeginAim()
        {
            if (_aimIndex >= _aimSequence.Count)
            {
                SendStagedPlay();
                return;
            }

            _aimDefinition = _aimSequence[_aimIndex];

            if (_aimDefinition.RequiredRow != RowType.None && _aimDefinition.RequiredRow != _stagedRow.BoardRow)
            {
                _aimIndex++;
                BeginAim();
                return;
            }

            _allowPartial = _aimDefinition.AllowPartial;
            _requiredTargets = _aimDefinition.Count;

            var anchor = new BoardCell(!_traitor, _stagedRow.BoardRow, _stagedSlot);
            _targetPool = _targetQuery.Targets(anchor, _aimDefinition);

            if (_targetPool.Count == 0)
            {
                if (_allowPartial)
                {
                    _aimIndex++;
                    BeginAim();
                    return;
                }

                PlacementDenied.OnNext(Unit.Default);
                CancelSelection();
                return;
            }

            _selectedTargets = new List<string>();
            _state = State.AimAfterPlacement;

            HighlightCells.OnNext(_targetQuery.Zone(anchor, _aimDefinition));
            HighlightPlacementCell.OnNext(null);
            HighlightTargets.OnNext(_targetPool);
        }

        private void SendStagedPlay()
        {
            var required = _aimSequence.Sum(aim => aim.Count);

            if (required > 0 && _confirmedTargets.Count < required)
                Log.Warn(LogTag.Cards,
                    $"{_selectedCard.Instance.Definition.Id} sent with {_confirmedTargets.Count}/{required} targets");

            CardPlayRequested.OnNext((
                _selectedCard.Instance,
                _stagedRow,
                _stagedSlot,
                new List<string>(_confirmedTargets)));

            FinishSelection();
        }

        private void OnDragCandidate(CardView card)
        {
            if (_matchState.IsPileWindowOpen.Value) return;

            if (_state == State.Picked && _selectedCard == card)
            {
                BeginDrag(card, _plan);
                return;
            }

            if (_state != State.Idle)
                CancelSelection();

            if (!CanSelect(card)) return;

            BeginDrag(card, BuildPlan(card.Instance.Definition));
        }

        private void BeginDrag(CardView card, Plan plan)
        {
            _inputRouter.CaptureDrag();

            _selectedCard = card;
            _plan = plan;
            _selectedTargets = new List<string>();
            _cardDetached = true;
            _state = State.Dragging;

            _previewService.Hide();
            DragBegan.OnNext(card);

            HighlightForPlan();
        }

        private void OnDragMoved(InputRouter.PointerHit hit)
        {
            if (_state != State.Dragging) return;

            Hover(hit);
        }

        private void OnPointerMoved(InputRouter.PointerHit hit)
        {
            if (_state != State.Picked) return;

            Hover(hit);
        }

        private void Hover(InputRouter.PointerHit hit)
        {
            switch (_plan)
            {
                case Plan.PlaceUnit:
                case Plan.AimUnit:
                    HoverSlot(hit.Slot);
                    return;
                case Plan.RowChoice:
                case Plan.Confirm:
                    HoverRow(hit.Slot != null ? hit.Slot.Row : hit.Row);
                    return;
            }
        }

        private void HoverRow(CardLaneView lane)
        {
            var row = lane as BoardRowView;

            if (row != null && !CanPlayOn(row))
                row = null;

            if (_hoveredRow == row) return;

            _hoveredRow = row;
            HighlightRowHover.OnNext(row);
        }

        private bool CanPlayOn(BoardRowView row)
            => _plan == Plan.RowChoice ? IsChoosableRow(row) : row.HasSlots;

        private void HoverSlot(SlotView slot)
        {
            var overFreeSlot = slot != null && IsPlacementRow(slot.Row) && slot.Row.IsSlotFree(slot.Index);

            var anchor = overFreeSlot
                ? new BoardCell(!_traitor, slot.Row.BoardRow, slot.Index)
                : default;

            if (_hoveredAnchorValid == overFreeSlot && (!overFreeSlot || _hoveredAnchor.Equals(anchor)))
                return;

            _hoveredAnchor = anchor;
            _hoveredAnchorValid = overFreeSlot;

            if (!overFreeSlot)
            {
                HighlightCells.OnNext(Array.Empty<BoardCell>());
                HighlightPlacementCell.OnNext(null);
                HighlightPredicted.OnNext(PlayPreviewQuery.Prediction.Empty);
                return;
            }

            SlotHovered.OnNext(Unit.Default);

            HighlightCells.OnNext(PlacementCells(anchor));
            HighlightPlacementCell.OnNext(anchor);

            HighlightPredicted.OnNext(_aimDefinition != null
                ? PlayPreviewQuery.Prediction.Empty
                : _playPreview.Predict(_selectedCard.Instance.Definition, anchor));
        }

        private List<BoardCell> PlacementCells(BoardCell anchor)
        {
            var cells = new List<BoardCell>();

            if (_aimDefinition == null) return cells;

            foreach (var cell in _targetQuery.Zone(anchor, _aimDefinition))
                if (!cell.Equals(anchor))
                    cells.Add(cell);

            return cells;
        }

        private void OnDragEnded(InputRouter.PointerHit hit)
        {
            if (_state != State.Dragging) return;

            _hoveredAnchorValid = false;
            _hoveredRow = null;
            HighlightRowHover.OnNext(null);
            HighlightCells.OnNext(Array.Empty<BoardCell>());
            HighlightPlacementCell.OnNext(null);
            HighlightPredicted.OnNext(PlayPreviewQuery.Prediction.Empty);

            switch (_plan)
            {
                case Plan.PlaceUnit:
                case Plan.AimUnit:
                    DropUnit(hit);
                    break;
                case Plan.ManualTargets:
                    DropOnTarget(hit);
                    break;
                case Plan.RowChoice:
                    DropOnChosenRow(hit);
                    break;
                default:
                    DropAnywhereOnBoard(hit);
                    break;
            }
        }

        private void DropUnit(InputRouter.PointerHit hit)
        {
            var slot = hit.Slot;

            if (slot == null || !IsPlacementRow(slot.Row) || !slot.Row.IsSlotFree(slot.Index))
            {
                if (slot != null && IsPlacementRow(slot.Row))
                    PlacementDenied.OnNext(Unit.Default);

                CancelSelection();
                return;
            }

            StagePlacement(slot.Row, slot.Index);
        }

        private void DropOnTarget(InputRouter.PointerHit hit)
        {
            var target = hit.Card;

            if (target == null
                || target.mode != CardMode.OnBoard
                || target.Instance is not UnitInstance
                || _targetPool == null
                || !_targetPool.Contains(target.Instance.Id.ToString()))
            {
                CancelSelection();
                return;
            }

            if (_requiredTargets <= 1 || _targetPool.Count <= 1)
            {
                _selectedTargets.Add(target.Instance.Id.ToString());
                ConfirmTargets();
                return;
            }

            _state = State.TargetSelection;

            ReturnCardToHand();
            _selectedCard.SetSelected(true);
            HighlightTargets.OnNext(_targetPool);

            OnBoardCardClicked(target);
        }

        private void DropOnChosenRow(InputRouter.PointerHit hit)
        {
            var row = hit.Slot != null ? hit.Slot.Row : hit.Row;

            PlayOnRow(row);
        }

        private void DropAnywhereOnBoard(InputRouter.PointerHit hit)
        {
            var row = hit.Slot != null ? hit.Slot.Row : hit.Row;
            var onBoardCard = hit.Card != null && hit.Card.mode == CardMode.OnBoard;

            if (!onBoardCard && row is not BoardRowView)
            {
                CancelSelection();
                return;
            }

            ConfirmPlay();
        }

        private bool IsPlacementRow(CardLaneView lane)
            => lane is BoardRowView row && row.HasSlots && row.OwnSide != _traitor;

        private bool IsChoosableRow(CardLaneView lane)
        {
            if (lane is not BoardRowView row || !row.HasSlots) return false;

            return _rowScope == RowScope.Any || row.OwnSide == (_rowScope == RowScope.Own);
        }

        private void ReturnCardToHand()
        {
            if (!_cardDetached) return;

            _cardDetached = false;
            PlacementCancelled.OnNext(Unit.Default);
        }

        private void CancelSelection()
        {
            ReturnCardToHand();
            ResetSelection();
            TryArmPendingPlay();
        }

        private void FinishSelection()
        {
            _cardDetached = false;
            ResetSelection();
        }

        private void ResetSelection()
        {
            _previewService.Hide();
            ClearHighlights.OnNext(Unit.Default);
            HighlightCells.OnNext(Array.Empty<BoardCell>());
            HighlightPlacementCell.OnNext(null);
            HighlightTargets.OnNext(new List<string>());
            HighlightPredicted.OnNext(PlayPreviewQuery.Prediction.Empty);
            _selectedTargets.Clear();
            _confirmedTargets.Clear();
            _targetPool = null;
            _aimDefinition = null;
            _aimSequence = new List<AimedTargetingDefinition>();
            _aimIndex = 0;
            _traitor = false;

            if (_selectedCard != null)
                _selectedCard.SetSelected(false);

            _selectedCard = null;
            _stagedRow = null;
            _stagedSlot = -1;
            _hoveredAnchorValid = false;
            _hoveredRow = null;
            HighlightRowHover.OnNext(null);
            _state = State.Idle;
        }

        public void Dispose() => _disposables.Dispose();
    }
}
