using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class CardDragPresenter : MonoBehaviour
    {
        public CardLaneView hand;
        public CardLaneView pendingLane;
        public RectTransform dragLayer;

        [Inject] private readonly CardSelectionService _selectionService;
        [Inject] private readonly CardViewRegistry _registry;
        [Inject] private readonly InputRouter _inputRouter;
        [Inject] private readonly MatchState _matchState;

        private CardView _dragged;
        private CardView _staged;

        private void Start()
        {
            _selectionService.DragBegan
                .Subscribe(BeginDrag)
                .AddTo(this);

            _selectionService.PlacementStaged
                .Subscribe(e => Stage(_registry.Get(e.Card.Id), e.Row, e.SlotIndex))
                .AddTo(this);

            _selectionService.PlacementCancelled
                .Subscribe(_ => ReturnToHand())
                .AddTo(this);

            _selectionService.CardPlayRequested
                .Subscribe(_ => EndDrag())
                .AddTo(this);

            _matchState.ActionTimedOut
                .Subscribe(_ => OnActionTimedOut())
                .AddTo(this);
        }

        private void Update()
        {
            if (_dragged == null) return;

            _dragged.transform.position = _inputRouter.PointerPosition;
        }

        private void BeginDrag(CardView view)
        {
            if (view == null) return;

            _dragged = view;
            _staged = null;

            view.SetHovered(false);
            view.SetSelected(false);

            var dragged = view.transform;
            var position = dragged.position;

            var previousRow = dragged.parent != null ? dragged.parent.GetComponent<CardLaneView>() : null;
            previousRow?.DetachCard(view.gameObject);

            dragged.SetParent(dragLayer != null ? dragLayer.transform : transform, false);
            dragged.SetAsLastSibling();
            dragged.position = position;
            dragged.localRotation = Quaternion.identity;

            SetRaycasts(view, false);
        }

        private void Stage(CardView view, BoardRowView row, int slotIndex)
        {
            EndDrag();

            if (view == null || row == null) return;

            view.SetSelected(false);
            view.mode = CardMode.OnBoard;
            row.PlaceCard(view.gameObject, slotIndex);

            _staged = view;
        }

        private void ReturnToHand()
        {
            var view = _dragged != null ? _dragged : _staged;

            EndDrag();
            _staged = null;

            if (view == null) return;

            view.SetSelected(false);
            view.mode = CardMode.InHand;

            var lane = _matchState.PendingPlays.Contains(view.Instance) ? pendingLane : hand;

            if (lane != null)
                lane.AddCard(view.gameObject);
        }

        private void OnActionTimedOut()
        {
            if (_staged == null) return;

            if (!_matchState.Hand.Contains(_staged.Instance)
                && !_matchState.PendingPlays.Contains(_staged.Instance))
            {
                _staged = null;
                return;
            }

            ReturnToHand();
        }

        private void EndDrag()
        {
            if (_dragged == null) return;

            SetRaycasts(_dragged, true);
            _dragged = null;
        }

        private static void SetRaycasts(CardView view, bool enabled)
            => view.Group().blocksRaycasts = enabled;
    }
}
