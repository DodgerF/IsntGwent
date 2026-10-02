using IsntGwent.Scripts.Localization;
using IsntGwent.Scripts.Accounts.Client;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Accounts.UI
{
    /// Ник и очки в шапке меню. Кнопки «Сменить имя» больше нет (2026-10-01): ник приезжает с itch.io.
    public class PlayerHeaderView : MonoBehaviour
    {
        [Inject] private PlayerAccount _account;

        [SerializeField] private TextMeshProUGUI nicknameText;
        [SerializeField] private TextMeshProUGUI pointsText;

        private void Start()
        {
            _account.Nickname
                .Subscribe(nickname => nicknameText.text = string.IsNullOrEmpty(nickname) ? Loc.T("No name") : nickname)
                .AddTo(this);

            _account.Points
                .CombineLatest(_account.Rank, (points, rank) => rank > 0
                    ? Loc.F("{0} pts  #{1}", points, rank)
                    : Loc.F("{0} pts", points))
                .Subscribe(text => pointsText.text = text)
                .AddTo(this);
        }
    }
}
