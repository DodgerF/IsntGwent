using System;
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
        IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
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

        private IDisposable _pressWatcher;
        private Vector2 _pressPosition;
        private float _pressTime;
        private bool _pressing;
        private bool _held;
        private bool _cancelled;
        private bool _hovered;

        public bool Interactable { get; set; } = true;

        public IObservable<Unit> Clicked => _clicked;
        public IObservable<Unit> HoldStarted => _holdStarted;
        public IObservable<Unit> HoldEnded => _holdEnded;

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressing = true;
            _held = false;
            _cancelled = false;
            _pressTime = Time.unscaledTime;
            _pressPosition = eventData.position;

            _pressWatcher?.Dispose();
            _pressWatcher = Observable.EveryUpdate()
                .Subscribe(_ => WatchPress());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pressing) return;

            StopWatching();
            _pressing = false;

            if (_held)
            {
                _held = false;
                _holdEnded.OnNext(Unit.Default);
                return;
            }

            if (_cancelled) return;

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

            if (!_pressing) return;

            Cancel();
        }

        private static bool IsPointingDevice(PointerEventData eventData)
        {
            return eventData is not ExtendedPointerEventData ext || ext.pointerType == UIPointerType.MouseOrPen;
        }

        private void WatchPress()
        {
            if (Vector2.Distance(PointerPosition(), _pressPosition) > DragThreshold)
            {
                Cancel();
                return;
            }

            if (Time.unscaledTime - _pressTime < HoldDelay) return;

            StopWatching();
            _held = true;
            _audio?.Play(holdSoundId);
            _holdStarted.OnNext(Unit.Default);
        }

        private void Cancel()
        {
            StopWatching();
            _cancelled = true;

            if (!_held) return;

            _held = false;
            _holdEnded.OnNext(Unit.Default);
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

            if (_held)
            {
                _held = false;
                _holdEnded.OnNext(Unit.Default);
            }

            _pressing = false;
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
