using IsntGwent.Scripts.Lobby.Core;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    [RequireComponent(typeof(Button))]
    public class LobbyEntryView : MonoBehaviour
    {
        public TextMeshProUGUI Text;
        public GameObject LockIcon;

        private LobbyData _lobbyData;
        private Button _button;
        private bool _canJoin;

        [Inject] private LobbyViewModel _vm;

        public void Setup(LobbyData lobbyData)
        {
            _button = gameObject.GetComponent<Button>();

            Refresh(lobbyData);

            _button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _vm.SelectLobby(_lobbyData);
                })
                .AddTo(this);

            _vm.CanCreateOrJoinLobby
                .Subscribe(value =>
                {
                    _canJoin = value;
                    ApplyInteractable();
                })
                .AddTo(this);
        }

        public void Refresh(LobbyData lobbyData)
        {
            _lobbyData = lobbyData;

            Text.text = lobbyData.Name + "  " + lobbyData.Players + "/" + lobbyData.MaxPlayers;
            LockIcon.SetActive(lobbyData.IsPrivate);

            ApplyInteractable();
        }

        private void ApplyInteractable()
        {
            if (_button == null) return;

            var isFull = _lobbyData.MaxPlayers > 0 && _lobbyData.Players >= _lobbyData.MaxPlayers;

            _button.interactable = _canJoin && !isFull;
            Text.alpha = _button.interactable ? 1f : 0.3f;
        }
    }
}
