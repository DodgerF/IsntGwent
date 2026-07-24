using System;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;
using ReadyMessage = IsntGwent.Scripts.Messages.ReadyMessage;

namespace IsntGwent.Scripts.Lobby.Server
{
    public class ServerHandler : IInitializable, IDisposable
    {
        public readonly Subject<(NetworkConnectionToClient conn, CreateLobbyMessage msg)> OnCreateLobby = new();
        public readonly Subject<(NetworkConnectionToClient conn, JoinLobbyMessage msg)> OnJoinLobby = new();
        public readonly Subject<NetworkConnectionToClient> OnReady = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            NetworkServer.RegisterHandler<CreateLobbyMessage>((conn, msg) => OnCreateLobby.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<JoinLobbyMessage>((conn, msg) => OnJoinLobby.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<ReadyMessage>((conn, _) => OnReady.OnNext(conn));
        }

        public void Dispose()
        {
            NetworkServer.UnregisterHandler<CreateLobbyMessage>();
            NetworkServer.UnregisterHandler<JoinLobbyMessage>();
            NetworkServer.UnregisterHandler<ReadyMessage>();
        }
    }
}
