using UnityEngine;
using UnityEngine.EventSystems;

namespace IsntGwent
{
    [RequireComponent (typeof (Card))]
    public class ParentSetter : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        private Transform _parent;
        private static Physics2DRaycaster _raycaster;
        private Card _card;
        private void Awake()
        {
            if (_raycaster == null ) 
            {
                _raycaster = Camera.main.GetComponent<Physics2DRaycaster>();
                _raycaster.enabled = false;
            }

            _card = GetComponent<Card>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _parent = transform.parent;
            transform.SetParent(_parent.parent);

            _parent.GetComponent<Row>().RemoveCard(_card);

            _raycaster.enabled = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (transform.parent == _parent.parent)
            {
                transform.parent = _parent;
                _parent.GetComponent<Row>().AddCard(_card);
            }
            else
            {
                _parent = transform.parent;
            }
           
            _raycaster.enabled = false;
        }
    }
}
