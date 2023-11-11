using MyEventBus;
using UnityEngine;

namespace IsntGwent
{ 
    /// <summary>
    /// Говорит, какой ряд выбирает игрок для разыгрывания карты
    /// </summary>
    //названия хуйня - переделать
    [RequireComponent (typeof(BoxCollider))]
    [RequireComponent(typeof(Row))]
    public class RowColliderTrigger : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Player _player;
        [SerializeField] private EventBus _eventBus;

        private Row _row;
        private BoxCollider _boxCollider;
        #endregion

        #region Unity events
        private void Awake()
        {
            if (_player == null) 
                _player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
            if (_eventBus == null)
                _eventBus = GameObject.FindGameObjectWithTag("EventBus").GetComponent<EventBus>();

            _row = GetComponent<Row>();
            _boxCollider = GetComponent<BoxCollider>();
        }
        private void Start()
        {
            _boxCollider.enabled = false;
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

        private void OnStartedDrag(PlayerHaveStartedDragCardSignal signal)
        {
            _boxCollider.enabled = true;
        }

        private void OnPlayed(PlayerHavePlayedCardSignal signal)
        {
            _boxCollider.enabled = false;
        }

        private void OnMouseOver()
        {
            if (_player.Row == _row) return;
            _player.Row = _row;
        }
        private void OnMouseExit()
        {
            _player.Row = null;
        }
    }
}
