using IsntGwent.Scripts.Localization;
using System;
using IsntGwent.Scripts.Accounts.Client;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Lobby.UI;
using IsntGwent.Scripts.Messages;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Accounts.UI
{
    public class NicknameWindow : MonoBehaviour
    {
        private static readonly TimeSpan CheckDelay = TimeSpan.FromMilliseconds(250);

        [Inject] private LobbyViewModel _vm;
        [Inject] private PlayerAccount _account;

        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private string testNickname;

        private string _checkedNickname;
        private AccountError _checkedError;
        private string _submitted;

        private void Start()
        {
            nicknameInput.placeholder.GetComponent<TextMeshProUGUI>().text = Loc.T("Your nickname");
            nicknameInput.characterLimit = NicknameRules.MaxLength;

            _vm.IsNicknameWindowOpen
                .Subscribe(OnWindowToggled)
                .AddTo(this);

            nicknameInput.onValueChanged.AsObservable()
                .Subscribe(_ => OnTyped())
                .AddTo(this);

            nicknameInput.onValueChanged.AsObservable()
                .Throttle(CheckDelay)
                .ObserveOnMainThread()
                .Subscribe(_ => RequestCheck())
                .AddTo(this);

            _account.OnNicknameChecked
                .Subscribe(OnChecked)
                .AddTo(this);

            confirmButton.OnClickAsObservable()
                .Subscribe(_ => Submit())
                .AddTo(this);

            _account.OnLoginFailed
                .Subscribe(ShowError)
                .AddTo(this);

            if (cancelButton != null)
            {
                cancelButton.OnClickAsObservable()
                    .Subscribe(_ => _vm.CloseNicknameWindow())
                    .AddTo(this);
            }
        }

        private void OnWindowToggled(bool isOpen)
        {
            gameObject.SetActive(isOpen);

            if (!isOpen) return;

            nicknameInput.text = _account.HasNickname || string.IsNullOrEmpty(testNickname)
                ? _account.Nickname.Value
                : testNickname;

            OnTyped();
            RequestCheck();

            if (cancelButton != null)
                cancelButton.gameObject.SetActive(_account.HasNickname);
        }

        private void Submit()
        {
            _submitted = NicknameRules.Trim(nicknameInput.text);
            _account.SetNickname(nicknameInput.text);
        }

        private void OnTyped()
        {
            _checkedNickname = null;
            _submitted = null;
            Refresh();
        }

        private void RequestCheck()
        {
            var nickname = NicknameRules.Trim(nicknameInput.text);
            if (!NicknameRules.IsValid(nickname)) return;

            if (_account.CheckNickname(nickname)) return;

            _checkedNickname = nickname;
            _checkedError = AccountError.None;
            Refresh();
        }

        private void OnChecked(NicknameCheckResultMessage msg)
        {
            _checkedNickname = msg.Nickname;
            _checkedError = msg.Error;
            Refresh();
        }

        private void Refresh()
        {
            var nickname = NicknameRules.Trim(nicknameInput.text);

            if (!NicknameRules.IsValid(nickname))
            {
                confirmButton.interactable = false;
                errorText.text = nickname.Length == 0 ? string.Empty : Describe(AccountError.BadNickname);
                return;
            }

            if (IsTestNickname(nickname))
            {
                confirmButton.interactable = true;
                errorText.text = string.Empty;
                return;
            }

            if (_checkedNickname != nickname)
            {
                confirmButton.interactable = false;
                errorText.text = string.Empty;
                return;
            }

            confirmButton.interactable = _checkedError == AccountError.None;
            errorText.text = _checkedError == AccountError.None ? string.Empty : Describe(_checkedError);
        }

        private bool IsTestNickname(string nickname)
        {
            return !string.IsNullOrEmpty(testNickname) && NicknameRules.Trim(testNickname) == nickname;
        }

        private void ShowError(AccountError error)
        {
            if (_submitted != NicknameRules.Trim(nicknameInput.text)) return;

            errorText.text = Describe(error);
        }

        private static string Describe(AccountError error)
        {
            return error switch
            {
                AccountError.BadNickname => Loc.F("Nickname must be {0} to {1} characters", NicknameRules.MinLength, NicknameRules.MaxLength),
                AccountError.AlreadyOnline => Loc.T("This nickname is already in use. Pick another one"),
                AccountError.NicknameTaken => Loc.T("Another account already uses this nickname. Pick another one"),
                _ => Loc.T("Could not sign in")
            };
        }
    }
}
