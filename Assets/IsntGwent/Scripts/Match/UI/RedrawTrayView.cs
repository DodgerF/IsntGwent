using System.Collections.Generic;
using DG.Tweening;
using IsntGwent.Scripts.Cards.UI;
using UnityEngine;

namespace IsntGwent.Scripts.Match.UI
{
    public class RedrawTrayView : MonoBehaviour
    {
        public float cardScale = 1.6f;
        public int cardsPerLine = 5;
        public float cardSpacing = 175f;
        public float lineSpacing = 260f;
        [SerializeField] private float edgeMargin = 60f;

        private readonly List<Transform> _slots = new();

        private RectTransform _rt;
        private Transform _flightChild;
        private float _flightDuration;

        public int CardCount
        {
            get
            {
                var count = 0;

                foreach (var slot in _slots)
                    if (slot != null)
                        count++;

                return count;
            }
        }

        private void Awake()
        {
            _rt = (RectTransform)transform;
        }

        public void AddCard(GameObject card)
        {
            var cardTransform = card.transform;
            var previousParent = cardTransform.parent;
            var previousRow = previousParent != null ? previousParent.GetComponent<CardLaneView>() : null;

            cardTransform.DOKill();

            var hasFlight = Application.isPlaying && previousParent != null;
            var start = cardTransform.position;

            cardTransform.SetParent(transform, false);

            TakeSlot(cardTransform);

            if (hasFlight)
            {
                cardTransform.position = start;

                _flightChild = cardTransform;
                _flightDuration = CardAnimConfig.PlayFlightDuration;
            }

            previousRow?.RefreshLayout();

            RefreshLayout();

            _flightChild = null;
        }

        public void Release(GameObject card)
        {
            var cardTransform = card.transform;

            cardTransform.DOKill();

            FreeSlot(cardTransform);
        }

        public void ReleaseAll()
        {
            foreach (var child in _slots)
            {
                if (child == null) continue;

                child.DOKill();
                ResetScale(child);
            }

            _slots.Clear();

            foreach (Transform child in transform)
            {
                child.DOKill();
                ResetScale(child);
            }
        }

        private static void ResetScale(Transform card)
        {
            if (card.TryGetComponent<CardView>(out var view))
                view.SetBaseScale(1f);
            else
                card.localScale = Vector3.one;
        }

        private void TakeSlot(Transform card)
        {
            if (_slots.Contains(card))
                return;

            for (var i = 0; i < _slots.Count; i++)
            {
                if (_slots[i] != null) continue;

                _slots[i] = card;
                return;
            }

            _slots.Add(card);
        }

        private void FreeSlot(Transform card)
        {
            var index = _slots.IndexOf(card);
            if (index < 0)
                return;

            _slots[index] = null;

            while (_slots.Count > 0 && _slots[^1] == null)
                _slots.RemoveAt(_slots.Count - 1);
        }

        public void RefreshLayout()
        {
            for (var i = 0; i < _slots.Count; i++)
                if (_slots[i] != null && _slots[i].parent != transform)
                    _slots[i] = null;

            var count = _slots.Count;
            if (count == 0)
                return;

            if (_rt == null)
                _rt = (RectTransform)transform;

            var perLine = Mathf.Max(cardsPerLine, 1);
            var lines = Mathf.CeilToInt(count / (float)perLine);
            perLine = Mathf.CeilToInt(count / (float)lines);

            var spacing = LineSpacingX(perLine);
            var startY = (lines - 1) * lineSpacing / 2f;

            for (var i = 0; i < count; i++)
            {
                var line = i / perLine;
                var indexInLine = i % perLine;
                var inThisLine = Mathf.Min(perLine, count - line * perLine);

                var startX = -((inThisLine - 1) * spacing) / 2f;
                var target = new Vector3(startX + indexInLine * spacing, startY - line * lineSpacing, 0f);

                var child = _slots[i];
                if (child == null) continue;

                var isFlight = child == _flightChild;

                MoveTo(
                    child,
                    target,
                    isFlight ? _flightDuration : CardAnimConfig.RowLayoutDuration,
                    isFlight ? CardAnimConfig.FlightEase : CardAnimConfig.RowLayoutEase);
            }
        }

        private float LineSpacingX(int perLine)
        {
            var available = _rt.rect.width - edgeMargin;
            if (available <= 0f)
                return cardSpacing;

            var required = (perLine - 1) * cardSpacing;

            if (perLine > 1 && required > available)
                return available / (perLine - 1);

            return cardSpacing;
        }

        private void MoveTo(Transform child, Vector3 target, float duration, Ease ease)
        {
            child.DOKill();

            if (child.TryGetComponent<CardView>(out var view))
                view.SetBaseScale(cardScale, duration, ease);
            else
                child.localScale = Vector3.one * cardScale;

            child.localRotation = Quaternion.identity;

            if (!Application.isPlaying || duration <= 0f)
            {
                child.localPosition = target;
                return;
            }

            child.DOLocalMove(target, duration).SetEase(ease);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                RefreshLayout();
        }
    }
}
