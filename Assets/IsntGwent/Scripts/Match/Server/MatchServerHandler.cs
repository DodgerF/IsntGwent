using System;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class MatchServerHandler : IInitializable, IDisposable
    {
        public readonly Subject<(NetworkConnectionToClient conn, PlayCardMessage msg)> OnPlayCard = new();
        public readonly Subject<NetworkConnectionToClient> OnPass = new();
        public readonly Subject<NetworkConnectionToClient> OnLeave = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            NetworkServer.RegisterHandler<PlayCardMessage>((conn, msg) => OnPlayCard.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<PassMessage>((conn, _) => OnPass.OnNext(conn));
            NetworkServer.RegisterHandler<LeaveMessage>((conn, _) => OnLeave.OnNext(conn));
        }

        public void Dispose()
        {
            NetworkServer.UnregisterHandler<PlayCardMessage>();
            NetworkServer.UnregisterHandler<PassMessage>();
            NetworkServer.UnregisterHandler<LeaveMessage>();
        }
    }
}
