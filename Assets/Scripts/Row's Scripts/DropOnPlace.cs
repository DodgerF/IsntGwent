using UnityEngine;
using UnityEngine.EventSystems;

namespace IsntGwent
{ 
    [RequireComponent (typeof(BoxCollider2D))]
    [RequireComponent(typeof(CardList))]
    public class DropOnPlace : MonoBehaviour, IDropHandler
    {
        private CardList _cards;
        private void Awake()
        {
            _cards = GetComponent<CardList>();
        }
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag.TryGetComponent(out ParentSetter card))
            {
                card.transform.SetParent(transform);
                card.transform.localPosition = Vector3.zero;

                _cards.AddCard(card.GetComponent<Card>());
            }
        }
    }
}
