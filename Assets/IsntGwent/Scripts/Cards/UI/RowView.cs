using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Services;
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
            card.transform.SetParent(null);
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            int count = transform.childCount;
            if (count == 0)
                return;

            float spacing = cardSpacing;
            float requiredWidth = (count - 1) * spacing;

            if (requiredWidth > maxWidth)
            {
                spacing = maxWidth / (count - 1);
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

        public void AddCard(GameObject card)
        {
            card.transform.SetParent(transform, false);
            RefreshLayout();
        }

        public void RemoveCard(GameObject card)
        {
            card.transform.SetParent(null);
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