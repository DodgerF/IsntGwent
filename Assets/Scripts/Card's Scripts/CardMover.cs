using UnityEngine;
using UnityEngine.EventSystems;

namespace IsntGwent
{
    [RequireComponent (typeof (Card))]
    public class CardMover : MonoBehaviour, IBeginDragHandler, IDragHandler, IDropHandler
    {
        #region Fields
        private Vector3 _offset;

        private Canvas _canvas;

        private string _moveLayer = "CardMove";
        private string _defaultLayer = "Default";
        #endregion

        #region Unity events
        private void Awake()
        {
            _canvas = GetComponentInChildren<Canvas>();

            _canvas.worldCamera = Camera.main;
        }
        #endregion

        #region Interface implementations
        public void OnBeginDrag(PointerEventData eventData)
        {
            _offset = transform.position - Camera.main.ScreenToWorldPoint(GetMousePos(eventData));
            //_canvas.sortingOrder = 1;
            _offset.z = 0f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 newPos = Camera.main.ScreenToWorldPoint(GetMousePos(eventData));
            transform.position = newPos + _offset;
        }

        public void OnDrop(PointerEventData eventData)
        {
            //_canvas.sortingOrder = 0;
        }
        #endregion

        #region Private methods
        private Vector3 GetMousePos(PointerEventData eventData)
        {
            Vector3 mousePos = eventData.position;
            mousePos.z = Camera.main.nearClipPlane - Camera.main.transform.position.z;
            return mousePos;
        } 
        #endregion


    }
}
