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

        [Inject] private LobbyViewModel _vm;
        
        public void Setup(LobbyData lobbyData)
        {
            if (_lobbyData.LobbyId == "") return;
            
            _lobbyData = lobbyData;
            Text.text = lobbyData.Name;
            LockIcon.SetActive(lobbyData.IsPrivate);

            var button =  gameObject.GetComponent<Button>();
            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _vm.SelectLobby(_lobbyData);
                })
                .AddTo(this);
            _vm.CanCreateOrJoinLobby
                .Subscribe(value =>
                {
                    button.interactable = value;
                    float alpha = button.interactable ? 1f : 0.5f;
                    Text.alpha = alpha;
                })
                .AddTo(this);
        }
    }
}