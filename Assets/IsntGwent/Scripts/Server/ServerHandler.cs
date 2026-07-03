using System;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Messages;
using Mirror;
using Zenject;
using ReadyMessage = IsntGwent.Scripts.Messages.ReadyMessage;

namespace IsntGwent.Scripts.Server
{
    public class ServerHandler : IInitializable, IDisposable
    {
        private readonly LobbyManager _lobbyManager;

        public ServerHandler(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
        }
        
        public void Initialize()
        {
            if (NetworkServer.active)
            {
                NetworkServer.RegisterHandler<CreateLobbyMessage>(OnCreateLobbyRequested);
                NetworkServer.RegisterHandler<JoinLobbyMessage>(OnJoinLobbyRequested);
                NetworkServer.RegisterHandler<ReadyMessage>(OnReady);
            }
        }

        private void OnReady(NetworkConnectionToClient conn, ReadyMessage _)
        {
            var lobbyId = _lobbyManager.GetLobbyId(conn);
            _lobbyManager.TryStartLobby(lobbyId);
        }

        private void OnJoinLobbyRequested(NetworkConnectionToClient conn, JoinLobbyMessage message)
        {
            var result = _lobbyManager.TryJoinLobby(conn, message);
            
            conn.Send(new JoinLobbyResultMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
            });
        }

        private void OnCreateLobbyRequested(NetworkConnectionToClient conn, CreateLobbyMessage message)
        {
            var result = _lobbyManager.TryCreateLobby(conn, message);
            
            conn.Send(new CreateLobbyResultMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
            });
            
        }


        public void Dispose()
        {
            
        }
    }
}