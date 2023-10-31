using UnityEngine;

namespace IsntGwent
{
    [RequireComponent (typeof (CardMover))]
    public class Card : MonoBehaviour
    {
        private CardMover _mover;

        private void Awake()
        {
            _mover = GetComponent<CardMover>();
        }
    }
}
