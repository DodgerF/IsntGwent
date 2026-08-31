using System;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Server
{
    public class AccountServerHandler : IInitializable, IDisposable
    {
        public readonly Subject<(NetworkConnectionToClient conn, LoginMessage msg)> OnLogin = new();
        public readonly Subject<(NetworkConnectionToClient conn, NicknameCheckMessage msg)> OnNicknameCheck = new();
        public readonly Subject<NetworkConnectionToClient> OnLeaderboardRequested = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            NetworkServer.RegisterHandler<LoginMessage>((conn, msg) => OnLogin.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<NicknameCheckMessage>((conn, msg) => OnNicknameCheck.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<LeaderboardRequestMessage>((conn, _) => OnLeaderboardRequested.OnNext(conn));
        }

        public void Dispose()
        {
            NetworkServer.UnregisterHandler<LoginMessage>();
            NetworkServer.UnregisterHandler<NicknameCheckMessage>();
            NetworkServer.UnregisterHandler<LeaderboardRequestMessage>();
        }
    }
}
