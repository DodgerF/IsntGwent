using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IsntGwent.Scripts.Cards.UI
{
    public class HandFocusZone : MonoBehaviour, IPointerClickHandler
    {
        public event Action Clicked;

        public void OnPointerClick(PointerEventData eventData)
        {
            var hit = eventData.pointerCurrentRaycast.gameObject;

            if (hit != null && hit.GetComponentInParent<CardView>() != null) return;

            Clicked?.Invoke();
        }
    }
}
