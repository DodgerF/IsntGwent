using UnityEngine;

namespace IsntGwent.Scripts.Cards.UI
{
    public class RowView : MonoBehaviour
    {
        public float cardSpacing = 105f;
        public float maxWidth = 1000f;

        private void RefreshLayout()
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