using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.UI;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts
{
    public class InputRouter : IInitializable, ITickable, IDisposable
    {
        [Inject] private readonly GraphicRaycaster _raycaster;
        [Inject] private readonly EventSystem _eventSystem;
        
        public readonly Subject<CardView> CardPressed = new();
        public readonly Subject<RowView> RowPressed = new();
        public readonly Subject<Unit> EmptyPressed = new();
        public readonly Subject<CardView> CardHovered = new();
        public readonly Subject<Unit> HoverEnded = new();
        public readonly Subject<CardView> BoardCardPressed = new();

        private readonly List<RaycastResult> _results = new();
        private float _pressTime;
        private bool _pressing;
        private Vector2 _pressPosition;
        private CardView _currentHoveredCard;
        private InputAction _pressAction;

        public void Initialize()
        {
            _pressAction = new InputAction(binding: "<Pointer>/press");
            _pressAction.started += _ => OnPressStarted();
            _pressAction.canceled += _ => OnPressEnded();
            _pressAction.Enable();
        }

        private void OnPressStarted()
        {
            _pressing = true;
            _pressTime = Time.time;
            _pressPosition = GetPointerPosition();
        }

        private void OnPressEnded()
        {
            if (!_pressing) return;

            var pressDuration = Time.time - _pressTime;
            var delta = Vector2.Distance(GetPointerPosition(), _pressPosition);
            var wasDrag = delta > 10f;
            var wasLongPress = pressDuration > 0.3f;

            if (_currentHoveredCard != null)
            {
                HoverEnded.OnNext(Unit.Default);
                _currentHoveredCard = null;
            }

            if (!wasDrag && !wasLongPress)
                HandleClick();

            _pressing = false;
        }

        public void Tick()
        {
            if (!_pressing) return;
            UpdateHover();
        }

        private void UpdateHover()
        {
            var card = RaycastCard();
            if (card == _currentHoveredCard) return;

            if (card != null)
            {
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
                EmptyPressed.OnNext(Unit.Default);
                return;
            }
            
            var top = _results[0].gameObject;
            
            if (top.GetComponentInParent<CardView>() is { } card)
            {
                if (card.mode == CardMode.OnBoard)
                    BoardCardPressed.OnNext(card);
                else
                    CardPressed.OnNext(card);
                return;
            }
            if (top.GetComponentInParent<RowView>() is { } row)
            {
                RowPressed.OnNext(row);
                return;
            }
            
            EmptyPressed.OnNext(Unit.Default);
        }

        private CardView RaycastCard()
        {
            Raycast();
            
            if (_results.Count == 0) return null;
            
            return _results[0].gameObject.GetComponentInParent<CardView>();
        }

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
        }
    }
}