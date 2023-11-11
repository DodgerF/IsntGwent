using UnityEngine;

namespace IsntGwent
{
    /// <summary>
    /// Передвижение каты и ее вид в это время.
    /// </summary>
    [RequireComponent (typeof (Card))]
    [RequireComponent (typeof (BoxCollider))]
    public class CardMover : MonoBehaviour
    {
        #region Fields
        private Canvas _canvas;
        private Vector3 _mousePos;

        private string _defaultLayer = "Default";
        private string _cardMoveLayer = "CardMove";
        #endregion

        #region Unity events
        private void Awake()
        {
            _canvas = GetComponentInChildren<Canvas>();
        }
        #endregion

        #region Private methods
        private Vector3 GetMousePos()
        {
            return Camera.main.WorldToScreenPoint(transform.position);
        }

        private void OnMouseDown()
        {
            _canvas.sortingLayerName = _cardMoveLayer;
            _mousePos = Input.mousePosition - GetMousePos();
        }

        private void OnMouseDrag() 
        {
            transform.position = Camera.main.ScreenToWorldPoint(Input.mousePosition - _mousePos);
        }

        private void OnMouseUp()
        {
            _canvas.sortingLayerName = _defaultLayer;
        }
        #endregion


    }
}
