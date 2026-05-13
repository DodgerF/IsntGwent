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

            gameObject.GetComponent<Button>().OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _vm.SelecteLobby(_lobbyData);
                })
                .AddTo(this);
        }
    }
}