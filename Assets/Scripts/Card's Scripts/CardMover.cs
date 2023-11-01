using UnityEngine;
using UnityEngine.EventSystems;

namespace IsntGwent
{
    public class CardMover : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private Vector3 _offset;

        public void OnBeginDrag(PointerEventData eventData)
        {
            _offset = transform.position - Camera.main.ScreenToWorldPoint(GetMousePos(eventData));
            _offset.z = 0;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 newPos = Camera.main.ScreenToWorldPoint(GetMousePos(eventData));
            transform.position = newPos + _offset;
        }

        private Vector3 GetMousePos(PointerEventData eventData)
        {
            Vector3 mousePos = eventData.position;
            mousePos.z = Camera.main.nearClipPlane - Camera.main.transform.position.z;
            return mousePos;
        }
    }
}
