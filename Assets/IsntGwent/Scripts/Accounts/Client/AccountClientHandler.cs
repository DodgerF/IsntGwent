using System;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Client
{
    public class AccountClientHandler : IInitializable, IDisposable
    {
        public readonly Subject<LoginResultMessage> OnLoginResult = new();
        public readonly Subject<NicknameCheckResultMessage> OnNicknameChecked = new();
        public readonly Subject<LeaderboardResultMessage> OnLeaderboard = new();

        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            MyNetManager.ClientConnected
                .Subscribe(_ => RegisterHandlers())
                .AddTo(_disposables);

            if (NetworkClient.active)
                RegisterHandlers();
        }

        public void SendLogin(string nickname)
        {
            if (!NetworkClient.isConnected) return;

            NetworkClient.Send(new LoginMessage { Nickname = nickname });
        }

        public void SendNicknameCheck(string nickname)
        {
            if (!NetworkClient.isConnected) return;

            NetworkClient.Send(new NicknameCheckMessage { Nickname = nickname });
        }

        public void RequestLeaderboard()
        {
            if (!NetworkClient.isConnected) return;

            NetworkClient.Send(new LeaderboardRequestMessage());
        }

        private void RegisterHandlers()
        {
            NetworkClient.ReplaceHandler<LoginResultMessage>(msg => OnLoginResult.OnNext(msg));
            NetworkClient.ReplaceHandler<NicknameCheckResultMessage>(msg => OnNicknameChecked.OnNext(msg));
            NetworkClient.ReplaceHandler<LeaderboardResultMessage>(msg => OnLeaderboard.OnNext(msg));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
