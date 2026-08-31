using IsntGwent.Scripts.Accounts.Client;
using IsntGwent.Scripts.Lobby.UI;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Accounts.UI
{
    public class PlayerHeaderView : MonoBehaviour
    {
        [Inject] private PlayerAccount _account;
        [Inject] private LobbyViewModel _vm;

        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI pointsText;
        [SerializeField] private Button changeNicknameButton;

        private void Start()
        {
            _account.Nickname
                .Subscribe(nickname => nicknameText.text = string.IsNullOrEmpty(nickname) ? "No name" : nickname)
                .AddTo(this);

            _account.Points
                .CombineLatest(_account.Rank, (points, rank) => rank > 0
                    ? $"{points} pts  #{rank}"
                    : $"{points} pts")
                .Subscribe(text => pointsText.text = text)
                .AddTo(this);

            if (changeNicknameButton == null) return;

            changeNicknameButton.OnClickAsObservable()
                .Subscribe(_ => _vm.OpenNicknameWindow())
                .AddTo(this);
        }
    }
}
