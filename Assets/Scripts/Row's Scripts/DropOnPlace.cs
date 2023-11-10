using UnityEngine;
using UnityEngine.EventSystems;

namespace IsntGwent
{ 
    [RequireComponent (typeof(BoxCollider2D))]
    [RequireComponent(typeof(Row))]
    public class DropOnPlace : MonoBehaviour, IDropHandler
    {
        private Row _cards;
        private void Awake()
        {
            _cards = GetComponent<Row>();
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
