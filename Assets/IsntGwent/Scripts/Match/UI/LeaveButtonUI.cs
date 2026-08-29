using UnityEngine;
using UnityEngine.UI;
using Zenject;
using IsntGwent.Scripts.Match.Client;
using IsntGwent.Scripts.Network;

namespace IsntGwent.Scripts.Match.UI
{
    [RequireComponent(typeof(Button))]
    public class LeaveButtonUI : MonoBehaviour
    {
        [Inject] private readonly MatchClientHandler _handler;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly ConnectionService _connectionService;
        [Inject] private readonly MatchReconnectService _reconnectService;
        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (!_matchState.IsConnectionLost.Value && !_reconnectService.IsReconnecting.Value)
                _handler.SendLeave();

            _reconnectService.EndSeat();
            _connectionService.ReturnToMenu();
        }
    }
}