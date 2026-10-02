using System;
using System.Globalization;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Accounts.Server.Itch;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Server
{
    public class AccountController : IInitializable, IDisposable
    {
        public const int TopCount = 5;

        [Inject] private readonly AccountServerHandler _handler;
        [Inject] private readonly AccountRegistry _registry;
        [Inject] private readonly Leaderboard _leaderboard;
        [Inject] private readonly ItchAuthService _itch;

        private readonly CompositeDisposable _disposables = new();

        private int _guestCounter;

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            // Личность ставится на рукопожатии соединения — см. AccountAuthenticator.
            AccountAuthenticator.ServerAuthenticate = Authenticate;

            _handler.OnLeaderboardRequested
                .Subscribe(SendLeaderboard)
                .AddTo(_disposables);

            MyNetManager.ServerDisconnected
                .Subscribe(conn => _registry.Logout(conn))
                .AddTo(_disposables);
        }

        /// <summary>
        /// Пустой токен — гость (редактор, десктопная сборка). Непустой — сессия, выданная
        /// ItchAuthService после того, как itch подтвердил, чей это ключ.
        /// Своего ника и своего id клиент не присылает вовсе: подделывать нечего.
        /// </summary>
        private AuthResponseMessage Authenticate(NetworkConnectionToClient conn, string session)
        {
            if (conn == null) return Reject(AccountError.Unknown);

            var account = string.IsNullOrEmpty(session)
                ? NewGuest()
                : _itch.Resolve(session);

            if (account == null) return Reject(AccountError.BadSession);

            if (_registry.IsOnlineElsewhere(account.Id, conn))
                return Reject(AccountError.AlreadyOnline);

            _registry.Login(conn, account);

            Log.Info(LogTag.Accounts, "signed in: " + account.Id + " (" + account.Nickname + ")");

            return new AuthResponseMessage
            {
                IsSuccess = true,
                Nickname = account.Nickname,
                Points = account.Points,
                Rank = account.IsGuest ? 0 : _leaderboard.RankOf(account.Id),
                IsGuest = account.IsGuest,
            };
        }

        /// <summary>
        /// Гость живёт только в памяти: в хранилище не пишется, в таблицу не попадает, очков не копит.
        /// Нужен затем, что itch неоткуда взять в редакторе и в десктопной сборке, а упираться
        /// в экран входа при разработке нельзя.
        /// </summary>
        private AccountData NewGuest()
        {
            _guestCounter++;

            return new AccountData
            {
                Id = ItchConfig.GuestIdPrefix + Guid.NewGuid().ToString("N"),
                Nickname = "Guest " + _guestCounter.ToString(CultureInfo.InvariantCulture),
                IsGuest = true,
            };
        }

        private static AuthResponseMessage Reject(AccountError error)
        {
            return new AuthResponseMessage { IsSuccess = false, Error = error };
        }

        private void SendLeaderboard(NetworkConnectionToClient conn)
        {
            if (conn == null) return;

            var top = _leaderboard.Top(TopCount);
            var entries = new LeaderboardEntry[top.Count];

            for (var i = 0; i < top.Count; i++)
                entries[i] = ToEntry(top[i]);

            var account = _registry.Resolve(conn);
            var isRanked = account != null && !account.IsGuest;

            conn.Send(new LeaderboardResultMessage
            {
                Top = entries,
                Me = isRanked ? ToEntry(account) : default,
                MyRank = isRanked ? _leaderboard.RankOf(account.Id) : 0,
            });
        }

        private static LeaderboardEntry ToEntry(AccountData account)
        {
            return new LeaderboardEntry
            {
                Nickname = account.Nickname,
                Points = account.Points,
                Wins = account.Wins,
                Losses = account.Losses,
                Ties = account.Ties,
            };
        }

        public void Dispose()
        {
            if (AccountAuthenticator.ServerAuthenticate == Authenticate)
                AccountAuthenticator.ServerAuthenticate = null;

            _registry.Clear();
            _disposables.Dispose();
        }
    }
}
