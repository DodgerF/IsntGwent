using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace IsntGwent.Scripts.UI
{
    public class AppScrollRect : ScrollRect
    {
        private const float FallbackDeltaPerTick = 6f;
        private const float SnapThreshold = 0.0005f;

        [SerializeField] private float wheelStep = 260f;
        [SerializeField] private float wheelDuration = 0.12f;

        private bool _wheeling;
        private bool _wheelVertical;
        private float _wheelTarget;
        private float _wheelVelocity;

        public override void OnScroll(PointerEventData eventData)
        {
            if (content == null || viewport == null) return;

            var ticks = eventData.scrollDelta / DeltaPerTick();
            var tick = Mathf.Abs(ticks.y) > Mathf.Abs(ticks.x) ? ticks.y : ticks.x;
            if (Mathf.Approximately(tick, 0f)) return;

            var useVertical = vertical && HiddenSize(true) > 0f;
            if (!useVertical && !(horizontal && HiddenSize(false) > 0f)) return;

            StopMovement();

            if (!_wheeling || _wheelVertical != useVertical)
            {
                _wheelVertical = useVertical;
                _wheelTarget = NormalizedPosition(useVertical);
                _wheelVelocity = 0f;
                _wheeling = true;
            }

            var shift = tick * wheelStep / HiddenSize(useVertical);
            _wheelTarget = Mathf.Clamp01(_wheelTarget + (useVertical ? shift : -shift));
        }

        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            if (!AllowsDrag(eventData)) return;

            _wheeling = false;
            base.OnInitializePotentialDrag(eventData);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!AllowsDrag(eventData)) return;

            _wheeling = false;
            base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (!AllowsDrag(eventData)) return;

            base.OnDrag(eventData);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (!AllowsDrag(eventData)) return;

            base.OnEndDrag(eventData);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            if (!_wheeling) return;

            if (content == null || viewport == null || HiddenSize(_wheelVertical) <= 0f)
            {
                _wheeling = false;
                return;
            }

            var current = NormalizedPosition(_wheelVertical);
            var next = Mathf.SmoothDamp(current, _wheelTarget, ref _wheelVelocity, wheelDuration,
                Mathf.Infinity, Time.unscaledDeltaTime);

            if (Mathf.Abs(_wheelTarget - next) < SnapThreshold)
            {
                next = _wheelTarget;
                _wheeling = false;
            }

            SetNormalizedPosition(_wheelVertical, next);
        }

        private static bool AllowsDrag(PointerEventData eventData) =>
            eventData is not ExtendedPointerEventData ext || ext.pointerType != UIPointerType.MouseOrPen;

        private static float DeltaPerTick()
        {
            var module = EventSystem.current != null
                ? EventSystem.current.currentInputModule as InputSystemUIInputModule
                : null;

            var perTick = module != null ? module.scrollDeltaPerTick : FallbackDeltaPerTick;

            return perTick > 0f ? perTick : FallbackDeltaPerTick;
        }

        private float HiddenSize(bool useVertical) =>
            useVertical
                ? Mathf.Max(0f, content.rect.height - viewport.rect.height)
                : Mathf.Max(0f, content.rect.width - viewport.rect.width);

        private float NormalizedPosition(bool useVertical) =>
            useVertical ? verticalNormalizedPosition : horizontalNormalizedPosition;

        private void SetNormalizedPosition(bool useVertical, float value)
        {
            if (useVertical)
                verticalNormalizedPosition = value;
            else
                horizontalNormalizedPosition = value;
        }
    }
}
