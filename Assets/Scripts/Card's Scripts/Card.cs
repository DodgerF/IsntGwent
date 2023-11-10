using UnityEngine;

namespace IsntGwent
{
    [RequireComponent (typeof (ParentSetter))]
    public class Card : MonoBehaviour
    {
        private static RectTransform _viewTransform;
        public static float Width => _viewTransform.rect.width;

        private CardMover _mover;

        private void Awake()
        {
            _mover = GetComponent<CardMover>();

            if (_viewTransform == null)
            {
                _viewTransform = GetComponentInChildren<RectTransform>();
            }
        }
    }
}
