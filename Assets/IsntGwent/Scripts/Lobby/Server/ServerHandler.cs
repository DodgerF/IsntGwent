using System;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;
using ReadyMessage = IsntGwent.Scripts.Messages.ReadyMessage;

namespace IsntGwent.Scripts.Lobby.Server
{
    public class ServerHandler : IInitializable, IDisposable
    {
        [Inject] private readonly SeatRegistry _seats;

        public readonly Subject<(NetworkConnectionToClient conn, CreateLobbyMessage msg)> OnCreateLobby = new();
        public readonly Subject<(NetworkConnectionToClient conn, JoinLobbyMessage msg)> OnJoinLobby = new();
        public readonly Subject<Seat> OnReady = new();
        public readonly Subject<(NetworkConnectionToClient conn, ReconnectRequestMessage msg)> OnReconnect = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            NetworkServer.RegisterHandler<CreateLobbyMessage>((conn, msg) => OnCreateLobby.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<JoinLobbyMessage>((conn, msg) => OnJoinLobby.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<ReadyMessage>((conn, _) => Publish(OnReady, conn));
            NetworkServer.RegisterHandler<ReconnectRequestMessage>((conn, msg) => OnReconnect.OnNext((conn, msg)));
        }

        private void Publish(Subject<Seat> subject, NetworkConnectionToClient conn)
        {
            var seat = _seats.Resolve(conn);
            if (seat == null) return;

            subject.OnNext(seat);
        }

        public void Dispose()
        {
            NetworkServer.UnregisterHandler<CreateLobbyMessage>();
            NetworkServer.UnregisterHandler<JoinLobbyMessage>();
            NetworkServer.UnregisterHandler<ReadyMessage>();
            NetworkServer.UnregisterHandler<ReconnectRequestMessage>();
        }
    }
}
