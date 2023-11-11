using UnityEngine;

namespace IsntGwent
{
    [RequireComponent (typeof (BoxCollider))]
    public class Card : MonoBehaviour
    {
        private static RectTransform _viewTransform;
        public static float Width => _viewTransform.rect.width;

        private void Awake()
        {
            if (_viewTransform == null)
            {
                _viewTransform = GetComponentInChildren<RectTransform>();
            }
        }
    }
}
