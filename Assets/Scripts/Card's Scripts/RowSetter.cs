using MyEventBus;
using UnityEngine;

namespace IsntGwent
{
    /// <summary>
    /// Устанавливает ряд и родителя для карты.
    /// </summary>
    [RequireComponent (typeof (CardMover))]
    public class RowSetter : MonoBehaviour
    {
        #region Fields
        private Transform _parent;
        private Card _card;

        private BoxCollider _boxCollider;

        [SerializeField] private Player _player;
        [SerializeField] private EventBus _eventBus;
        #endregion

        #region Unity events
        private void Awake()
        {

            _card = GetComponent<Card>();
            _boxCollider = GetComponent<BoxCollider>();

            if (_player == null)
                _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
            if (_eventBus == null)
                _eventBus = GameObject.FindGameObjectWithTag("EventBus").GetComponent<EventBus>();
        }

        #region (Un)Subscribe
        private void OnEnable()
        {
            _eventBus.Subscribe<PlayerHavePlayedCardSignal>(OnPlayed);
            _eventBus.Subscribe<PlayerHaveStartedDragCardSignal>(OnStartedDrag);
        }
        private void OnDisable()
        {
            _eventBus.Unsubscribe<PlayerHavePlayedCardSignal>(OnPlayed);
            _eventBus.Unsubscribe<PlayerHaveStartedDragCardSignal>(OnStartedDrag);
        }  
        #endregion

        #endregion

        private void OnPlayed(PlayerHavePlayedCardSignal signal)
        {
            _boxCollider.enabled = true;
        }

        private void OnStartedDrag(PlayerHaveStartedDragCardSignal signal)
        {
            _boxCollider.enabled = false;
        }

        public void OnMouseDown()
        {
            _parent = transform.parent;
            transform.SetParent(_parent.parent);

            _parent.GetComponent<Row>().RemoveCard(_card);

            _player.DragCard(_card);
        }

        private void OnMouseUp()
        {
            if (_player.Row == null)
            {
                transform.parent = _parent;
                _parent.GetComponent<Row>().AddCard(_card);
            }
            else
            {
                _parent = _player.Row.transform;

                _card.transform.SetParent(_parent);
                _card.transform.localPosition = Vector3.zero;
            }

            _player.PlayCard();

        }
    }
}
