using MyEventBus;
using UnityEngine;

namespace IsntGwent
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private EventBus _eventBus;

        private Card _draggableCard;
        public Card DraggableCard => _draggableCard;

        [HideInInspector] public Row Row;


        private void Awake()
        {
            if (_eventBus == null)
                _eventBus = GameObject.FindGameObjectWithTag("EventBus").GetComponent<EventBus>();
        }

        public void PlayCard()
        {
            _eventBus.Invoke(new PlayerHavePlayedCardSignal(_draggableCard, Row));

            if (Row == null) return;

            Row.AddCard(_draggableCard);

            _draggableCard = null;
            Row = null;
        }

        public void DragCard(Card card)
        {
            _draggableCard = card;

            _eventBus.Invoke(new PlayerHaveStartedDragCardSignal(_draggableCard));
        }
    }
}
