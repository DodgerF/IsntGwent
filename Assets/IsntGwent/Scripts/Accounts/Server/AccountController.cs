using System;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using Mirror;
using UniRx;
using IsntGwent.Scripts.Diagnostics;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Server
{
    public class AccountController : IInitializable, IDisposable
    {
        public const int TopCount = 10;

        [Inject] private readonly AccountServerHandler _handler;
        [Inject] private readonly AccountRegistry _registry;
        [Inject] private readonly IAccountStore _store;
        [Inject] private readonly Leaderboard _leaderboard;

        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            _handler.OnLogin
                .Subscribe(t => OnLogin(t.conn, t.msg))
                .AddTo(_disposables);

            _handler.OnNicknameCheck
                .Subscribe(t => OnNicknameCheck(t.conn, t.msg))
                .AddTo(_disposables);

            _handler.OnLeaderboardRequested
                .Subscribe(SendLeaderboard)
                .AddTo(_disposables);

            MyNetManager.ServerDisconnected
                .Subscribe(conn => _registry.Logout(conn))
                .AddTo(_disposables);
        }

        private void OnLogin(NetworkConnectionToClient conn, LoginMessage msg)
        {
            if (conn == null) return;

            if (!NicknameRules.IsValid(msg.Nickname))
            {
                Reject(conn, AccountError.BadNickname);
                return;
            }

            var id = NicknameRules.Normalize(msg.Nickname);

            if (_registry.IsOnlineElsewhere(id, conn))
            {
                Reject(conn, AccountError.AlreadyOnline);
                return;
            }

            var nickname = NicknameRules.Trim(msg.Nickname);
            var current = _registry.Resolve(conn);

            var account = current != null && current.Id != id
                ? Rename(conn, current, id, nickname)
                : Enter(conn, id, nickname);

            if (account == null) return;

            _registry.Login(conn, account);

            conn.Send(new LoginResultMessage
            {
                IsSuccess = true,
                Nickname = account.Nickname,
                Points = account.Points,
                Rank = _leaderboard.RankOf(account.Id),
            });
        }

        private void OnNicknameCheck(NetworkConnectionToClient conn, NicknameCheckMessage msg)
        {
            if (conn == null) return;

            conn.Send(new NicknameCheckResultMessage
            {
                Nickname = NicknameRules.Trim(msg.Nickname),
                Error = CheckNickname(conn, msg.Nickname),
            });
        }

        private AccountError CheckNickname(NetworkConnectionToClient conn, string nickname)
        {
            if (!NicknameRules.IsValid(nickname)) return AccountError.BadNickname;

            var id = NicknameRules.Normalize(nickname);
            var current = _registry.Resolve(conn);

            if (current != null && current.Id == id) return AccountError.None;
            if (_registry.IsOnlineElsewhere(id, conn)) return AccountError.AlreadyOnline;
            if (_store.Find(id) != null) return AccountError.NicknameTaken;

            return AccountError.None;
        }

        private AccountData Rename(NetworkConnectionToClient conn, AccountData account, string id, string nickname)
        {
            if (!_store.Rename(account, id, nickname))
            {
                Reject(conn, AccountError.NicknameTaken);
                return null;
            }

            _leaderboard.Touch();
            Log.Info(LogTag.Accounts, "Account renamed to " + id);

            return account;
        }

        private AccountData Enter(NetworkConnectionToClient conn, string id, string nickname)
        {
            var isNew = _store.Find(id) == null;
            var account = _store.GetOrCreate(id, nickname);

            if (account == null)
            {
                Reject(conn, AccountError.Unknown);
                return null;
            }

            if (isNew || account.Nickname != nickname)
            {
                account.Nickname = nickname;
                _store.Save(account);
                _leaderboard.Touch();
            }

            return account;
        }

        private static void Reject(NetworkConnectionToClient conn, AccountError error)
        {
            conn.Send(new LoginResultMessage { IsSuccess = false, Error = error });
        }

        private void SendLeaderboard(NetworkConnectionToClient conn)
        {
            if (conn == null) return;

            var top = _leaderboard.Top(TopCount);
            var entries = new LeaderboardEntry[top.Count];

            for (var i = 0; i < top.Count; i++)
                entries[i] = ToEntry(top[i]);

            var account = _registry.Resolve(conn);

            conn.Send(new LeaderboardResultMessage
            {
                Top = entries,
                Me = account != null ? ToEntry(account) : default,
                MyRank = account != null ? _leaderboard.RankOf(account.Id) : 0,
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
            _registry.Clear();
            _disposables.Dispose();
        }
    }
}
