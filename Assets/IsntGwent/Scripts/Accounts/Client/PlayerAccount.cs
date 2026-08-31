using System;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using Mirror;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Client
{
    public class PlayerAccount : IInitializable, IDisposable
    {
        private const string NicknameKey = "account.nick";

        private static readonly TimeSpan TakenRetryInterval = TimeSpan.FromSeconds(5);

        [Inject] private readonly AccountClientHandler _handler;

        public readonly ReactiveProperty<string> Nickname = new(string.Empty);
        public readonly ReactiveProperty<int> Points = new(0);
        public readonly ReactiveProperty<int> Rank = new(0);
        public readonly ReactiveProperty<bool> IsLoggedIn = new(false);
        public readonly Subject<AccountError> OnLoginFailed = new();
        public readonly Subject<Unit> OnLoggedIn = new();
        public readonly Subject<NicknameCheckResultMessage> OnNicknameChecked = new();

        private readonly CompositeDisposable _disposables = new();
        private readonly SerialDisposable _retry = new();

        private string _pending;

        public bool HasNickname => !string.IsNullOrEmpty(Nickname.Value);

        public void Initialize()
        {
            if (NetworkServer.active) return;

            _retry.AddTo(_disposables);

            Nickname.Value = PlayerPrefs.GetString(NicknameKey, string.Empty);

            _handler.OnLoginResult
                .Subscribe(OnResult)
                .AddTo(_disposables);

            _handler.OnNicknameChecked
                .Subscribe(msg => OnNicknameChecked.OnNext(msg))
                .AddTo(_disposables);

            _handler.OnLeaderboard
                .Where(_ => IsLoggedIn.Value)
                .Subscribe(OnLeaderboard)
                .AddTo(_disposables);

            MyNetManager.ClientConnected
                .Subscribe(_ => TryLogin())
                .AddTo(_disposables);

            MyNetManager.ClientDisconnected
                .Subscribe(_ => IsLoggedIn.Value = false)
                .AddTo(_disposables);

            TryLogin();
        }

        public void SetNickname(string nickname)
        {
            _retry.Disposable = null;
            _pending = NicknameRules.Trim(nickname);

            if (!NicknameRules.IsValid(_pending))
            {
                OnLoginFailed.OnNext(AccountError.BadNickname);
                return;
            }

            _handler.SendLogin(_pending);
        }

        public bool CheckNickname(string nickname)
        {
            if (!NetworkClient.isConnected) return false;

            _handler.SendNicknameCheck(NicknameRules.Trim(nickname));
            return true;
        }

        private void ScheduleRetry()
        {
            _retry.Disposable = Observable
                .Timer(TakenRetryInterval)
                .Subscribe(_ => TryLogin());
        }

        private void TryLogin()
        {
            var nickname = string.IsNullOrEmpty(_pending) ? Nickname.Value : _pending;
            if (string.IsNullOrEmpty(nickname)) return;

            _handler.SendLogin(nickname);
        }

        private void OnLeaderboard(LeaderboardResultMessage msg)
        {
            if (string.IsNullOrEmpty(msg.Me.Nickname)) return;

            Points.Value = msg.Me.Points;
            Rank.Value = msg.MyRank;
        }

        private void OnResult(LoginResultMessage msg)
        {
            if (!msg.IsSuccess)
            {
                IsLoggedIn.Value = false;

                if (msg.Error == AccountError.AlreadyOnline)
                    ScheduleRetry();
                else
                    _pending = null;

                OnLoginFailed.OnNext(msg.Error);
                return;
            }

            _retry.Disposable = null;
            _pending = null;

            Nickname.Value = msg.Nickname;
            Points.Value = msg.Points;
            Rank.Value = msg.Rank;
            IsLoggedIn.Value = true;

            PlayerPrefs.SetString(NicknameKey, msg.Nickname ?? string.Empty);
            PlayerPrefs.Save();

            OnLoggedIn.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
