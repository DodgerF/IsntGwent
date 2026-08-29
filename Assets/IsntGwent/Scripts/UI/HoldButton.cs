using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Audio;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Zenject;

namespace IsntGwent.Scripts.UI
{
    public class HoldButton : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float HoldDelay = 0.3f;
        private const float DragThreshold = 10f;

        public string clickSoundId = "ui_click";
        public string holdSoundId = "ui_hover";
        public string hoverSoundId = "ui_hover";
        public string hoverOutSoundId = "ui_hover_out";
        public string blockedSoundId = "ui_denied";

        [Inject] private readonly AudioService _audio;

        private readonly Subject<Unit> _clicked = new();
        private readonly Subject<Unit> _holdStarted = new();
        private readonly Subject<Unit> _holdEnded = new();

        private readonly List<RaycastResult> _hits = new();

        private PointerEventData _pointerData;
        private IDisposable _pressWatcher;
        private Vector2 _pressPosition;
        private float _pressTime;
        private bool _pressing;
        private bool _held;
        private bool _showing;
        private bool _cancelled;
        private bool _hovered;
        private HoldButton _target;
        private GameObject _routedDrag;

        public bool Interactable { get; set; } = true;

        public IObservable<Unit> Clicked => _clicked;
        public IObservable<Unit> HoldStarted => _holdStarted;
        public IObservable<Unit> HoldEnded => _holdEnded;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            _pressing = true;
            _held = false;
            _cancelled = false;
            _pressTime = Time.unscaledTime;
            _pressPosition = eventData.position;
            _routedDrag = null;

            _pressWatcher?.Dispose();
            _pressWatcher = Observable.EveryUpdate()
                .Subscribe(_ => WatchPress());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!_pressing) return;

            StopWatching();
            _pressing = false;

            if (_held)
            {
                EndSession();
                return;
            }

            if (_cancelled || eventData.dragging) return;

            if (!Interactable)
            {
                _audio?.Play(blockedSoundId);
                return;
            }

            _audio?.Play(clickSoundId);
            _clicked.OnNext(Unit.Default);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!IsPointingDevice(eventData)) return;
            if (IsPointerPressed()) return;

            _hovered = true;
            _audio?.Play(hoverSoundId);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hovered)
            {
                _hovered = false;
                _audio?.Play(hoverOutSoundId);
            }

            if (!_pressing || _held) return;

            Cancel();
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (transform.parent == null) return;

            ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_held) return;

            Cancel();

            if (transform.parent == null) return;

            _routedDrag = ExecuteEvents.ExecuteHierarchy(transform.parent.gameObject, eventData, ExecuteEvents.beginDragHandler);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_routedDrag == null) return;

            ExecuteEvents.Execute(_routedDrag, eventData, ExecuteEvents.dragHandler);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_routedDrag == null) return;

            ExecuteEvents.Execute(_routedDrag, eventData, ExecuteEvents.endDragHandler);
            _routedDrag = null;
        }

        private static bool IsPointingDevice(PointerEventData eventData)
        {
            return eventData is not ExtendedPointerEventData ext || ext.pointerType == UIPointerType.MouseOrPen;
        }

        private static bool IsPointerPressed() =>
            Pointer.current != null && Pointer.current.press.isPressed;

        private void WatchPress()
        {
            if (_held)
            {
                TrackTarget();
                return;
            }

            if (Vector2.Distance(PointerPosition(), _pressPosition) > DragThreshold)
            {
                Cancel();
                return;
            }

            if (Time.unscaledTime - _pressTime < HoldDelay) return;

            BeginSession();
        }

        private void BeginSession()
        {
            _held = true;
            SetTarget(this);
        }

        private void EndSession()
        {
            if (!_held) return;

            _held = false;
            SetTarget(null);
        }

        private void TrackTarget()
        {
            var button = ButtonUnderPointer();
            if (button == _target) return;

            SetTarget(button);
        }

        private void SetTarget(HoldButton button)
        {
            if (_target != null)
                _target.CloseHold();

            _target = button;

            if (_target != null)
                _target.OpenHold();
        }

        private void OpenHold()
        {
            if (_showing) return;

            _showing = true;
            _audio?.Play(holdSoundId);
            _holdStarted.OnNext(Unit.Default);
        }

        private void CloseHold()
        {
            if (!_showing) return;

            _showing = false;
            _holdEnded.OnNext(Unit.Default);
        }

        private HoldButton ButtonUnderPointer()
        {
            var events = EventSystem.current;
            if (events == null) return null;

            _pointerData ??= new PointerEventData(events);
            _pointerData.position = PointerPosition();

            _hits.Clear();
            events.RaycastAll(_pointerData, _hits);

            if (_hits.Count == 0) return null;

            var button = _hits[0].gameObject.GetComponentInParent<HoldButton>();

            return button != null && button.isActiveAndEnabled ? button : null;
        }

        private void Cancel()
        {
            StopWatching();
            _cancelled = true;
        }

        private void StopWatching()
        {
            _pressWatcher?.Dispose();
            _pressWatcher = null;
        }

        private static Vector2 PointerPosition() =>
            Pointer.current?.position.ReadValue() ?? Vector2.zero;

        private void OnDisable()
        {
            StopWatching();
            _hovered = false;
            _pressing = false;
            _routedDrag = null;

            CloseHold();
            EndSession();
        }

        private void OnDestroy()
        {
            StopWatching();
            _clicked.Dispose();
            _holdStarted.Dispose();
            _holdEnded.Dispose();
        }
    }
}
