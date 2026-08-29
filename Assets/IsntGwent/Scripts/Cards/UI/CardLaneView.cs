using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace IsntGwent.Scripts.Cards.UI
{
    public class CardLaneView : MonoBehaviour
    {
        public float cardSpacing = 105f;
        public float cardScale = 1f;
        public float maxWidth = 1000f;
        [SerializeField] protected float edgeMargin = 120f;

        public float fanSpread;
        public float fanAngleStep = 6f;
        public float fanArc;
        public float fanClosedFactor = 1f;
        public float fanClosedLift;
        public float fanLoweredLift;

        public float focusScale;
        public float focusOffsetX;
        public float focusSpacing = 230f;
        public float focusWidth = 1700f;
        public float focusArc = -70f;
        public float focusLift = 300f;

        protected RectTransform _rt;

        protected readonly List<Transform> _cards = new();

        protected Transform _flightChild;
        protected float _flightDuration;

        private bool _fanOpen = true;
        private bool _fanLowered;
        private bool _focused;
        private Transform _raisedCard;
        private float _raisedLift;

        public int CardCount => _cards.Count;

        public IReadOnlyList<Transform> Cards => _cards;

        public Transform RaisedCard => _raisedCard;

        public void SetRaised(Transform card, bool raised, float lift)
        {
            var target = raised ? card : null;

            if (_raisedCard == target && Mathf.Approximately(_raisedLift, lift)) return;
            if (!raised && _raisedCard != card) return;

            _raisedCard = target;
            _raisedLift = raised ? lift : 0f;
            RefreshLayout();
        }

        public void SetLowered(bool lowered)
        {
            if (_fanLowered == lowered) return;

            _fanLowered = lowered;
            RefreshLayout();
        }

        public void SetFanOpen(bool open)
        {
            if (_fanOpen == open) return;

            _fanOpen = open;
            RefreshLayout();
        }

        public bool IsFocused => _focused;

        public void SetFocused(bool focused)
        {
            if (_focused == focused) return;

            _focused = focused;
            RefreshLayout();
        }

        protected virtual void Awake()
        {
            _rt = (RectTransform)transform;

            foreach (Transform child in transform)
                if (IsLayoutChild(child))
                    _cards.Add(child);
        }

        protected virtual bool IsLayoutChild(Transform child)
            => child.GetComponent<HandOutlineLayer>() == null;

        protected virtual void ForgetPlacement(Transform card)
        {
        }

        protected virtual void ClearPlacements()
        {
        }

        public void DetachCard(GameObject card)
        {
            card.transform.DOKill();
            _cards.Remove(card.transform);
            ForgetPlacement(card.transform);
            card.transform.SetParent(null, false);
            RefreshLayout();
        }

        public void AddCard(GameObject card) => Attach(card, -1);

        public void AddCardAt(GameObject card, int orderIndex) => Attach(card, orderIndex);

        protected void Attach(GameObject card, int orderIndex)
        {
            var cardTransform = card.transform;
            var previousParent = cardTransform.parent;

            var previousLane = previousParent != null
                ? previousParent.GetComponent<CardLaneView>()
                : null;

            cardTransform.DOKill();

            var hasFlight = Application.isPlaying && previousParent != null;
            var start = cardTransform.position;

            previousLane?._cards.Remove(cardTransform);
            previousLane?.ForgetPlacement(cardTransform);
            cardTransform.SetParent(transform, false);

            _cards.Remove(cardTransform);

            if (orderIndex >= 0 && orderIndex < _cards.Count)
                _cards.Insert(orderIndex, cardTransform);
            else
                _cards.Add(cardTransform);

            PlaceAttached(cardTransform);

            if (hasFlight)
            {
                cardTransform.position = start;

                _flightChild = cardTransform;
                _flightDuration = previousLane == this
                    ? CardAnimConfig.RowLayoutDuration
                    : CardAnimConfig.PlayFlightDuration;
            }

            RefreshLayout();

            _flightChild = null;

            if (previousLane != null && previousLane != this)
                previousLane.RefreshLayout();
        }

        protected virtual void PlaceAttached(Transform card)
        {
        }

        public void RemoveCard(GameObject card)
        {
            card.transform.DOKill();
            _cards.Remove(card.transform);
            ForgetPlacement(card.transform);
            card.transform.SetParent(null, false);
            Destroy(card);
            RefreshLayout();
        }

        public void ClearCards()
        {
            foreach (Transform child in transform)
            {
                if (!IsLayoutChild(child)) continue;

                child.DOKill();
                Destroy(child.gameObject);
            }

            _cards.Clear();
            ClearPlacements();
        }

        public void RestoreOrder()
        {
            foreach (var card in _cards)
                if (card != null && card.parent == transform)
                    card.SetAsLastSibling();
        }

        public void RefreshLayout()
        {
            _cards.RemoveAll(c => c == null || c.parent != transform);

            if (_raisedCard != null && _raisedCard.parent != transform)
                _raisedCard = null;

            LayoutCards();
        }

        protected virtual void LayoutCards()
        {
            var count = _cards.Count;
            if (count == 0)
                return;

            if (_rt == null)
                _rt = (RectTransform)transform;

            var openness = _fanOpen || _focused ? 1f : Mathf.Clamp01(fanClosedFactor);
            var spacing = (_focused ? FocusSpacing(count) : LaneSpacing(count)) * openness;
            var startX = -((count - 1) * spacing) / 2f + (_focused ? focusOffsetX : 0f);
            var half = FanHalfSpread(count) * openness;
            var arc = (_focused ? focusArc : fanArc) * openness;
            var lift = _focused ? focusLift : _fanOpen ? 0f : fanClosedLift;
            var lowered = _fanLowered && !_focused ? fanLoweredLift : 0f;

            for (var i = 0; i < count; i++)
            {
                var child = _cards[i];
                var isFlight = child == _flightChild;
                var offset = count > 1 ? i / (float)(count - 1) * 2f - 1f : 0f;

                var isRaised = child == _raisedCard;

                var y = isRaised
                    ? lift + _raisedLift
                    : arc * offset * offset + lift + lowered;

                MoveTo(
                    child,
                    new Vector3(startX + i * spacing, y, 0f),
                    isFlight ? _flightDuration : CardAnimConfig.RowLayoutDuration,
                    isFlight ? CardAnimConfig.FlightEase : CardAnimConfig.RowLayoutEase,
                    isRaised ? 0f : -offset * half);
            }
        }

        private float FanHalfSpread(int count)
        {
            if (count < 2 || Mathf.Approximately(fanSpread, 0f)) return 0f;

            var spread = Mathf.Min(Mathf.Abs(fanSpread), Mathf.Abs(fanAngleStep) * (count - 1));

            return spread * Mathf.Sign(fanSpread) / 2f;
        }

        private float FocusSpacing(int count)
        {
            if (count < 2) return 0f;

            var limit = focusWidth > 0f ? focusWidth : LaneSpacing(count) * (count - 1);

            return Mathf.Min(focusSpacing, limit / (count - 1));
        }

        private float LaneSpacing(int count)
        {
            var limit = AvailableWidth();
            var spacing = cardSpacing;

            if (count > 1 && (count - 1) * spacing > limit)
                spacing = limit / (count - 1);

            return spacing;
        }

        protected float AvailableWidth()
        {
            var available = _rt.rect.width - edgeMargin;
            if (available <= 0f)
                available = maxWidth;

            return maxWidth > 0f ? Mathf.Min(available, maxWidth) : available;
        }

        protected void MoveTo(Transform child, Vector3 target, float duration, Ease ease, float angle = 0f)
        {
            child.DOKill();

            ApplyCardScale(child, duration, ease);

            if (!Application.isPlaying || duration <= 0f)
            {
                child.localPosition = target;
                child.localRotation = Quaternion.Euler(0f, 0f, angle);
                return;
            }

            child.DOLocalMove(target, duration).SetEase(ease);
            RotateTo(child, angle, duration, ease);
        }

        private static void RotateTo(Transform child, float angle, float duration, Ease ease)
        {
            var from = SignedAngle(child.localEulerAngles.z);

            if (Mathf.Approximately(from, angle)) return;

            DOTween.To(
                    () => SignedAngle(child.localEulerAngles.z),
                    value => child.localRotation = Quaternion.Euler(0f, 0f, value),
                    angle,
                    duration)
                .SetEase(ease)
                .SetTarget(child);
        }

        private static float SignedAngle(float angle) => Mathf.Repeat(angle + 180f, 360f) - 180f;

        protected void ApplyCardScale(Transform child, float duration, Ease ease)
        {
            var scale = _focused && focusScale > 0f ? focusScale : cardScale;

            if (child.TryGetComponent<CardView>(out var view))
                view.SetBaseScale(scale, duration, ease);
            else
                child.localScale = Vector3.one * scale;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                RefreshLayout();
        }
    }
}
