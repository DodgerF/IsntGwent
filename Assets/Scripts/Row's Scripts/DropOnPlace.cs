using UnityEngine;
using UnityEngine.EventSystems;

namespace IsntGwent
{
    [RequireComponent (typeof (BoxCollider2D))]
    public class DropOnPlace : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.pointerDrag.TryGetComponent<ParentSetter>(out ParentSetter card))
            {
                Debug.Log(gameObject.name);
                card.transform.SetParent(transform);
            }
        }
    }
}
