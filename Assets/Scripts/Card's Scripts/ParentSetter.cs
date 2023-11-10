using UnityEngine;
using UnityEngine.EventSystems;

namespace IsntGwent
{
    public class ParentSetter : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        private Transform _parent;
        private static Physics2DRaycaster _raycaster;
        private void Awake()
        {
            if (_raycaster == null ) 
            {
                _raycaster = Camera.main.GetComponent<Physics2DRaycaster>();
                _raycaster.enabled = false;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {

            _parent = transform.parent;
            transform.SetParent(_parent.parent);

            _parent.GetComponent<CardList>().RemoveCard(GetComponent<Card>());

            _raycaster.enabled = true;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _parent = transform.parent;
            _raycaster.enabled = false;
        }
    }
}
