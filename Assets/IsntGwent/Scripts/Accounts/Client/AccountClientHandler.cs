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

        public void RequestLeaderboard()
        {
            if (!NetworkClient.isConnected) return;

            NetworkClient.Send(new LeaderboardRequestMessage());
        }

        private void RegisterHandlers()
        {
            NetworkClient.ReplaceHandler<LeaderboardResultMessage>(msg => OnLeaderboard.OnNext(msg));
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
