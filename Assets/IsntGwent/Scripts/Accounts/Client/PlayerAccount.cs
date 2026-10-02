using System;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Client
{
    /// <summary>
    /// Личность игрока на клиенте. Сама она ничего не решает: и ник, и очки, и место приезжают
    /// с сервера в ответе на рукопожатие — клиент только показывает то, что ему сказали.
    /// </summary>
    public class PlayerAccount : IInitializable, IDisposable
    {
        [Inject] private readonly AccountClientHandler _handler;
        [Inject] private readonly ItchLoginService _login;
        [Inject] private readonly ConnectionService _connection;

        public readonly ReactiveProperty<string> Nickname = new(string.Empty);
        public readonly ReactiveProperty<int> Points = new(0);
        public readonly ReactiveProperty<int> Rank = new(0);
        public readonly ReactiveProperty<bool> IsLoggedIn = new(false);
        public readonly ReactiveProperty<bool> IsGuest = new(false);
        public readonly Subject<AccountError> OnLoginFailed = new();
        public readonly Subject<Unit> OnLoggedIn = new();

        private readonly CompositeDisposable _disposables = new();

        /// Браузерному игроку без сессии itch нужен вход: соединение он поднимет гостем,
        /// но в меню его встретит окно входа, и играть его не пустят.
        public readonly ReactiveProperty<bool> NeedsItchLogin = new(false);

        public void Initialize()
        {
            if (NetworkServer.active) return;

            // Чем представляемся при рукопожатии. Пусто = гость (редактор и десктоп).
            AccountAuthenticator.ClientToken = () => _login.Session;

            _login.SessionToken
                .Subscribe(token => NeedsItchLogin.Value = ItchLoginService.IsRequired && string.IsNullOrEmpty(token))
                .AddTo(_disposables);

            AccountAuthenticator.ClientResult
                .Subscribe(OnAuthResult)
                .AddTo(_disposables);

            // Вошли через itch — соединение переподнимается уже с сессией: личность живёт
            // на соединении, поменять её на лету нельзя.
            _login.OnSession
                .Subscribe(_ => _connection.Restart())
                .AddTo(_disposables);

            _handler.OnLeaderboard
                .Where(_ => IsLoggedIn.Value)
                .Subscribe(OnLeaderboard)
                .AddTo(_disposables);

            MyNetManager.ClientDisconnected
                .Subscribe(_ => IsLoggedIn.Value = false)
                .AddTo(_disposables);
        }

        public void SignIn()
        {
            _login.Begin();
        }

        private void OnAuthResult(AuthResponseMessage msg)
        {
            if (!msg.IsSuccess)
            {
                IsLoggedIn.Value = false;

                // Сервера, который знал бы эту сессию, больше нет: стираем токен и возвращаемся
                // гостем, чтобы игрок попал в меню и увидел там предложение войти заново.
                if (msg.Error == AccountError.BadSession)
                    _login.Forget();

                OnLoginFailed.OnNext(msg.Error);
                return;
            }

            Nickname.Value = msg.Nickname ?? string.Empty;
            Points.Value = msg.Points;
            Rank.Value = msg.Rank;
            IsGuest.Value = msg.IsGuest;
            IsLoggedIn.Value = true;

            OnLoggedIn.OnNext(Unit.Default);
        }

        private void OnLeaderboard(LeaderboardResultMessage msg)
        {
            if (string.IsNullOrEmpty(msg.Me.Nickname)) return;

            Points.Value = msg.Me.Points;
            Rank.Value = msg.MyRank;
        }

        public void Dispose()
        {
            AccountAuthenticator.ClientToken = null;

            _disposables.Dispose();
        }
    }
}
