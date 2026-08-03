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
        public readonly Subject<(NetworkConnectionToClient conn, RedrawCardMessage msg)> OnRedrawCard = new();
        public readonly Subject<NetworkConnectionToClient> OnRedrawReady = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            NetworkServer.RegisterHandler<PlayCardMessage>((conn, msg) => OnPlayCard.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<PassMessage>((conn, _) => OnPass.OnNext(conn));
            NetworkServer.RegisterHandler<LeaveMessage>((conn, _) => OnLeave.OnNext(conn));
            NetworkServer.RegisterHandler<RedrawCardMessage>((conn, msg) => OnRedrawCard.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<RedrawReadyMessage>((conn, _) => OnRedrawReady.OnNext(conn));
        }

        public void Dispose()
        {
            NetworkServer.UnregisterHandler<PlayCardMessage>();
            NetworkServer.UnregisterHandler<PassMessage>();
            NetworkServer.UnregisterHandler<LeaveMessage>();
            NetworkServer.UnregisterHandler<RedrawCardMessage>();
            NetworkServer.UnregisterHandler<RedrawReadyMessage>();
        }
    }
}
