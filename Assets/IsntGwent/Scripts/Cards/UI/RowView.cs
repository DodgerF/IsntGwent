using System.Collections.Generic;
using DG.Tweening;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Client;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    [RequireComponent(typeof(Image))]
    public class RowView : MonoBehaviour
    {
        [InjectOptional] private CardSelectionService _selectionService;

        public RowType row;
        private Image _image;
        private readonly Color _baseColor = new (0.3f, 0.3f, 0.3f,0.3f);
        private readonly Color _selectedColor = new (0.6f, 0.75f, 0.95f, 0.3f);

        public float cardSpacing = 105f;
        public float maxWidth = 1000f;
        [SerializeField] private float edgeMargin = 120f;

        private RectTransform _rt;

        private readonly List<Transform> _cards = new();

        private Transform _flightChild;
        private float _flightDuration;

        public int CardCount => _cards.Count;

        private void Awake()
        {
            _rt = (RectTransform)transform;

            foreach (Transform child in transform)
                _cards.Add(child);
        }

        private void Start()
        {
            _image = GetComponent<Image>();
            _image.color = _baseColor;

            _selectionService?.HighlightRows
                .Subscribe(card =>
                {
                    if (card is UnitDefinition unit)
                        SetHighlight(unit.Row == row);
                    else
                        SetHighlight(false);
                })
                .AddTo(this);

            _selectionService?.ClearHighlights
                .Subscribe(_ => SetHighlight(false))
                .AddTo(this);
        }

        private void SetHighlight(bool active)
        {
            _image.color = active ? _selectedColor : _baseColor;
        }

        public void DetachCard(GameObject card)
        {
            card.transform.DOKill();
            _cards.Remove(card.transform);
            card.transform.SetParent(null, false);
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            _cards.RemoveAll(c => c == null || c.parent != transform);

            int count = _cards.Count;
            if (count == 0)
                return;

            if (_rt == null)
                _rt = (RectTransform)transform;

            float available = _rt.rect.width - edgeMargin;
            if (available <= 0f)
                available = maxWidth;

            float limit = maxWidth > 0f ? Mathf.Min(available, maxWidth) : available;

            float spacing = cardSpacing;
            float requiredWidth = (count - 1) * spacing;

            if (count > 1 && requiredWidth > limit)
            {
                spacing = limit / (count - 1);
            }

            float startX = -((count - 1) * spacing) / 2f;

            for (int i = 0; i < count; i++)
            {
                var child = _cards[i];
                bool isFlight = child == _flightChild;

                var target = new Vector3(startX + i * spacing, 0f, 0f);

                MoveTo(
                    child,
                    target,
                    isFlight ? _flightDuration : CardAnimConfig.RowLayoutDuration,
                    isFlight ? CardAnimConfig.FlightEase : CardAnimConfig.RowLayoutEase);
            }
        }

        private static void MoveTo(Transform child, Vector3 target, float duration, Ease ease)
        {
            child.DOKill();

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

        public void AddCard(GameObject card)
        {
            var cardTransform = card.transform;
            var previousParent = cardTransform.parent;

            var previousRow = previousParent != null
                ? previousParent.GetComponent<RowView>()
                : null;

            cardTransform.DOKill();

            bool hasFlight = Application.isPlaying && previousParent != null;
            var start = cardTransform.position;

            previousRow?._cards.Remove(cardTransform);
            cardTransform.SetParent(transform, false);

            if (!_cards.Contains(cardTransform))
                _cards.Add(cardTransform);

            if (hasFlight)
            {
                cardTransform.position = start;

                _flightChild = cardTransform;
                _flightDuration = previousRow == this
                    ? CardAnimConfig.RowLayoutDuration
                    : CardAnimConfig.PlayFlightDuration;
            }

            RefreshLayout();

            _flightChild = null;

            if (previousRow != null && previousRow != this)
                previousRow.RefreshLayout();
        }

        public void RemoveCard(GameObject card)
        {
            card.transform.DOKill();
            _cards.Remove(card.transform);
            card.transform.SetParent(null, false);
            Destroy(card);
            RefreshLayout();
        }

        public void ClearCards()
        {
            foreach (Transform child in transform)
            {
                child.DOKill();
                Destroy(child.gameObject);
            }

            _cards.Clear();
        }
    }

}
