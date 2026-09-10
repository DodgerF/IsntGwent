using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.UI;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Core
{
    public class InputRouter : IInitializable, ITickable, IDisposable
    {
        [Inject] private readonly GraphicRaycaster _raycaster;
        [Inject] private readonly EventSystem _eventSystem;
        
        public readonly Subject<GameObject> Pressed = new();
        public readonly Subject<CardView> CardPressed = new();
        public readonly Subject<CardView> CardInspected = new();
        public readonly Subject<CardLaneView> RowPressed = new();
        public readonly Subject<SlotView> SlotPressed = new();
        public readonly Subject<Unit> EmptyPressed = new();
        public readonly Subject<CardView> CardHovered = new();
        public readonly Subject<Unit> HoverEnded = new();
        public readonly Subject<CardView> BoardCardPressed = new();
        public readonly Subject<int> Swiped = new();

        public readonly Subject<CardView> DragCandidate = new();
        public readonly Subject<PointerHit> DragMoved = new();
        public readonly Subject<PointerHit> DragEnded = new();
        public readonly Subject<PointerHit> PointerMoved = new();

        public readonly struct PointerHit
        {
            public readonly Vector2 Position;
            public readonly CardView Card;
            public readonly SlotView Slot;
            public readonly CardLaneView Row;

            public PointerHit(Vector2 position, CardView card, SlotView slot, CardLaneView row)
            {
                Position = position;
                Card = card;
                Slot = slot;
                Row = row;
            }
        }

        private const float ClickMoveThreshold = 10f;
        private const float DragPullRatio = 0.2f;
        private const float DragConeDegrees = 70f;
        private const float HoldDuration = 0.3f;
        private const float DoubleClickInterval = 0.3f;
        private const float HoverMoveThreshold = 4f;
        private const float SwipeThreshold = 60f;
        private const float ScrollThreshold = 0.01f;

        private readonly List<RaycastResult> _results = new();
        private float _pressTime;
        private bool _pressing;
        private Vector2 _pressPosition;
        private CardView _currentHoveredCard;
        private InputAction _pressAction;
        private InputAction _inspectAction;
        private CardView _heldCard;
        private bool _dragOffered;
        private bool _dragCaptured;
        private CardView _lastClickCard;
        private float _lastClickTime;
        private Vector2 _hoverPosition;

        public Vector2 PointerPosition => GetPointerPosition();

        public bool TrackHover { get; set; }

        public bool DoubleTapInspect { get; set; } = true;

        public void CaptureDrag() => _dragCaptured = true;

        public void Initialize()
        {
            _pressAction = new InputAction(binding: "<Pointer>/press");
            _pressAction.started += _ => OnPressStarted();
            _pressAction.canceled += _ => OnPressEnded();
            _pressAction.Enable();

            _inspectAction = new InputAction(binding: "<Mouse>/rightButton");
            _inspectAction.started += _ => OnInspectPressed();
            _inspectAction.Enable();
        }

        private void OnPressStarted()
        {
            _pressing = true;
            _pressTime = Time.time;
            _pressPosition = GetPointerPosition();
            _heldCard = RaycastCard();
            _dragOffered = false;
            _dragCaptured = false;
        }

        private void OnPressEnded()
        {
            if (!_pressing) return;

            if (_dragCaptured)
            {
                DragEnded.OnNext(BuildHit());
                ResetPress();
                return;
            }

            var pressDuration = Time.time - _pressTime;
            var delta = Vector2.Distance(GetPointerPosition(), _pressPosition);
            var wasDrag = delta > ClickMoveThreshold;
            var wasLongPress = pressDuration > HoldDuration;

            if (_currentHoveredCard != null)
            {
                HoverEnded.OnNext(Unit.Default);
                _currentHoveredCard = null;
            }

            if (!wasLongPress)
            {
                if (wasDrag)
                    RaiseSwipe();
                else
                    HandleClick();
            }

            ResetPress();
        }

        private void ResetPress()
        {
            _pressing = false;
            _heldCard = null;
            _dragOffered = false;
            _dragCaptured = false;
        }

        public void Tick()
        {
            if (!_pressing)
            {
                UpdateScroll();
                TrackPointer();
                return;
            }

            if (_dragCaptured)
            {
                DragMoved.OnNext(BuildHit());
                return;
            }

            OfferDrag();

            if (_dragCaptured) return;

            UpdateHover();
        }

        private void RaiseSwipe()
        {
            var delta = GetPointerPosition() - _pressPosition;

            if (Mathf.Abs(delta.y) < SwipeThreshold) return;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) return;

            Swiped.OnNext(delta.y > 0f ? 1 : -1);
        }

        private void UpdateScroll()
        {
            var scroll = Mouse.current?.scroll.ReadValue().y ?? 0f;

            if (Mathf.Abs(scroll) < ScrollThreshold) return;
            if (ScrollBelongsToWidget()) return;

            Swiped.OnNext(scroll > 0f ? 1 : -1);
        }

        private bool ScrollBelongsToWidget()
        {
            Raycast();

            if (_results.Count == 0) return false;

            var top = _results[0].gameObject;

            return top.GetComponentInParent<ScrollRect>() != null
                   || top.GetComponentInParent<CardPreviewWindow>() != null;
        }

        private void TrackPointer()
        {
            if (!TrackHover) return;
            if (!IsMousePointer()) return;

            var position = GetPointerPosition();

            if ((position - _hoverPosition).sqrMagnitude < HoverMoveThreshold) return;

            _hoverPosition = position;
            PointerMoved.OnNext(BuildHit());
        }

        private void OfferDrag()
        {
            if (_dragOffered) return;
            if (_heldCard == null || _heldCard.mode != CardMode.InHand) return;

            var delta = GetPointerPosition() - _pressPosition;

            if (delta.y < PullThreshold(_heldCard)) return;
            if (Vector2.Angle(Vector2.up, delta) > DragConeDegrees) return;

            _dragOffered = true;
            DragCandidate.OnNext(_heldCard);

            if (!_dragCaptured) return;

            if (_currentHoveredCard != null)
            {
                HoverEnded.OnNext(Unit.Default);
                _currentHoveredCard = null;
            }

            DragMoved.OnNext(BuildHit());
        }

        private static float PullThreshold(CardView card)
        {
            var rect = (RectTransform)card.transform;
            return rect.rect.height * rect.lossyScale.y * DragPullRatio;
        }

        private PointerHit BuildHit()
        {
            var position = GetPointerPosition();

            Raycast();

            if (_results.Count == 0)
                return new PointerHit(position, null, null, null);

            var top = _results[0].gameObject;

            return new PointerHit(
                position,
                top.GetComponentInParent<CardView>(),
                top.GetComponentInParent<SlotView>(),
                top.GetComponentInParent<CardLaneView>());
        }

        private void UpdateHover()
        {
            var card = RaycastCard();

            if (Time.time - _pressTime < HoldDuration) return;

            if (card != null && card.mode == CardMode.InHand)
                _heldCard = card;

            if (card == _currentHoveredCard) return;

            if (card != null)
            {
                card.PlayHoverSfx();

                CardHovered.OnNext(card);
                _currentHoveredCard = card;
            }
            else
            {
                HoverEnded.OnNext(Unit.Default);
                _currentHoveredCard = null;
            }
        }

        private void HandleClick()
        {
            Raycast();
            
            if (_results.Count == 0)
            {
                Pressed.OnNext(null);
                EmptyPressed.OnNext(Unit.Default);
                return;
            }
            
            var top = _results[0].gameObject;
            Pressed.OnNext(top);
            
            if (top.GetComponentInParent<CardView>() is { } card)
            {
                if (InsidePreview(card)) return;

                if (card.mode != CardMode.InHand)
                    BoardCardPressed.OnNext(card);
                else if (IsDoubleTap(card))
                    CardInspected.OnNext(card);
                else
                    CardPressed.OnNext(card);
                return;
            }
            if (top.GetComponentInParent<SlotView>() is { } slot)
            {
                SlotPressed.OnNext(slot);
                return;
            }
            if (top.GetComponentInParent<CardLaneView>() is { } row)
            {
                RowPressed.OnNext(row);
                return;
            }
            
            EmptyPressed.OnNext(Unit.Default);
        }

        private bool IsDoubleTap(CardView card)
        {
            if (!DoubleTapInspect || IsMousePointer()) return false;

            var isDouble = card == _lastClickCard && Time.time - _lastClickTime <= DoubleClickInterval;

            _lastClickCard = isDouble ? null : card;
            _lastClickTime = Time.time;

            return isDouble;
        }

        private static bool IsMousePointer() => Pointer.current is Mouse;

        private void OnInspectPressed()
        {
            var card = RaycastCard();
            if (card == null) return;

            CardInspected.OnNext(card);
        }

        private CardView RaycastCard()
        {
            Raycast();

            if (_results.Count == 0) return null;

            var card = _results[0].gameObject.GetComponentInParent<CardView>();

            return InsidePreview(card) ? null : card;
        }

        private static bool InsidePreview(CardView card) =>
            card != null && card.GetComponentInParent<CardPreviewWindow>() != null;

        private void Raycast()
        {
            _results.Clear();
            var pointerData = new PointerEventData(_eventSystem) { position = GetPointerPosition() };

            _raycaster.Raycast(pointerData, _results);
        }

        private Vector2 GetPointerPosition() =>
            Pointer.current?.position.ReadValue() ?? Vector2.zero;

        public void Dispose()
        {
            _pressAction?.Disable();
            _pressAction?.Dispose();
            _inspectAction?.Disable();
            _inspectAction?.Dispose();
        }
    }
}