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

        private void Awake()
        {
            _rt = (RectTransform)transform;
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
            card.transform.SetParent(null, false);
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            int count = transform.childCount;
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
                var child = transform.GetChild(i);

                var pos = child.localPosition;
                pos.x = startX + i * spacing;

                child.localPosition = pos;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                RefreshLayout();
        }

        public void AddCard(GameObject card)
        {
            var previousRow = card.transform.parent != null
                ? card.transform.parent.GetComponent<RowView>()
                : null;

            card.transform.SetParent(transform, false);
            RefreshLayout();

            if (previousRow != null && previousRow != this)
                previousRow.RefreshLayout();
        }

        public void RemoveCard(GameObject card)
        {
            card.transform.SetParent(null, false);
            Destroy(card);
            RefreshLayout();
        }

        public void ClearCards()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

}