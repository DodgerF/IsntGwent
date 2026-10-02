using IsntGwent.Scripts.Accounts.Client;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Localization;
using IsntGwent.Scripts.Lobby.UI;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Accounts.UI
{
    /// <summary>
    /// Окно входа. Ника здесь больше не спрашивают вовсе: он приезжает с itch.io вместе с личностью,
    /// а игрок жмёт одну кнопку. Поле ввода осталось объектом сцены от прежнего окна и гасится.
    /// </summary>
    public class ItchLoginWindow : MonoBehaviour
    {
        [Inject] private LobbyViewModel _vm;
        [Inject] private PlayerAccount _account;
        [Inject] private ItchLoginService _login;

        [SerializeField] private TMP_InputField nicknameInput;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI errorText;

        private TextMeshProUGUI _confirmLabel;
        private AccountError _lastError = AccountError.None;

        private void Start()
        {
            if (nicknameInput != null)
                nicknameInput.gameObject.SetActive(false);

            _confirmLabel = confirmButton.GetComponentInChildren<TextMeshProUGUI>(true);

            _vm.IsLoginWindowOpen
                .Subscribe(OnWindowToggled)
                .AddTo(this);

            confirmButton.OnClickAsObservable()
                .Subscribe(_ => SignIn())
                .AddTo(this);

            _login.State
                .Subscribe(_ => Refresh())
                .AddTo(this);

            _login.NeedsSecondClick
                .Subscribe(_ => Refresh())
                .AddTo(this);

            _account.OnLoginFailed
                .Subscribe(error =>
                {
                    _lastError = error;
                    Refresh();
                })
                .AddTo(this);

            if (cancelButton != null)
            {
                cancelButton.OnClickAsObservable()
                    .Subscribe(_ => _vm.CloseLoginWindow())
                    .AddTo(this);
            }

            Refresh();
        }

        private void OnWindowToggled(bool isOpen)
        {
            gameObject.SetActive(isOpen);

            if (!isOpen) return;

            // Закрыть окно может только тот, кому вход уже не нужен: иначе за ним меню,
            // в котором всё равно ничего нельзя.
            if (cancelButton != null)
                cancelButton.gameObject.SetActive(!_account.NeedsItchLogin.Value);

            // Вход начинается сам, без кнопки. Браузер откроет окно itch сразу, если игрок кликал
            // несколько секунд назад, иначе — по следующему клику в любом месте страницы
            // (это делает мост ItchAuth.jslib). Кнопка остаётся только для повтора после сбоя.
            if (_account.NeedsItchLogin.Value && _login.State.Value == ItchLoginState.Idle)
                SignIn();

            Refresh();
        }

        private void SignIn()
        {
            _lastError = AccountError.None;
            _account.SignIn();
        }

        private void Refresh()
        {
            // Вход идёт сам, жать нечего: кнопка появляется только после сбоя, чтобы повторить.
            confirmButton.gameObject.SetActive(_login.State.Value == ItchLoginState.Failed);

            if (_confirmLabel != null)
                _confirmLabel.text = Loc.T("Sign in with itch.io");

            errorText.text = Describe();
        }

        private string Describe()
        {
            if (_lastError != AccountError.None) return DescribeAccount(_lastError);

            if (_login.NeedsSecondClick.Value)
                return Loc.T("Click anywhere to connect your itch.io account");

            return _login.State.Value switch
            {
                ItchLoginState.Waiting => Loc.T("Connecting to your itch.io account…"),
                ItchLoginState.Failed => DescribeLogin(_login.Error.Value),
                _ => string.Empty
            };
        }

        private static string DescribeLogin(string reason)
        {
            return reason switch
            {
                "popup blocked" => Loc.T("Your browser blocked the sign-in window"),
                "timed out" => Loc.T("Signing in took too long. Try again"),
                _ => Loc.T("Could not sign in")
            };
        }

        private static string DescribeAccount(AccountError error)
        {
            return error switch
            {
                AccountError.BadSession => Loc.T("Your sign-in expired. Sign in again"),
                AccountError.AlreadyOnline => Loc.T("This account is already playing somewhere else"),
                AccountError.NotReady => Loc.T("The server is starting up. Try again in a moment"),
                _ => Loc.T("Could not sign in")
            };
        }
    }
}
