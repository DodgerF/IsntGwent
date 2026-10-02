using System;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Server
{
    /// <summary>
    /// Только транспорт. Вход сюда больше не приходит: личность ставит AccountAuthenticator
    /// на рукопожатии соединения, а здесь остаётся таблица лучших.
    /// </summary>
    public class AccountServerHandler : IInitializable, IDisposable
    {
        public readonly Subject<NetworkConnectionToClient> OnLeaderboardRequested = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            NetworkServer.RegisterHandler<LeaderboardRequestMessage>((conn, _) => OnLeaderboardRequested.OnNext(conn));
        }

        public void Dispose()
        {
            NetworkServer.UnregisterHandler<LeaderboardRequestMessage>();
        }
    }
}
