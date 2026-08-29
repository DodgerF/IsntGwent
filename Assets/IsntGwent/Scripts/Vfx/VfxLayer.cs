using UnityEngine;

namespace IsntGwent.Scripts.Vfx
{
    [RequireComponent(typeof(RectTransform))]
    public class VfxLayer : MonoBehaviour
    {
        public RectTransform Root => (RectTransform)transform;
    }
}
