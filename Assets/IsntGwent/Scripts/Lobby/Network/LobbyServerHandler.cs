using System;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Services;
using IsntGwent.Scripts.Messages;
using Mirror;
using Zenject;

namespace IsntGwent.Scripts.Lobby.Network
{
    public class LobbyServerHandler : IInitializable, IDisposable
    {
        private LobbyManager _lobbyManager;

        public LobbyServerHandler(LobbyManager lobbyManager)
        {
            _lobbyManager = lobbyManager;
        }
        
        public void Initialize()
        {
            if (NetworkServer.active)
            {
                NetworkServer.RegisterHandler<CreateLobbyMessage>(OnCreateLobbyRequested);
                NetworkServer.RegisterHandler<JoinLobbyMessage>(OnJoinLobbyRequested);
            }
        }

        private void OnJoinLobbyRequested(NetworkConnectionToClient conn, JoinLobbyMessage message)
        {
            var result = _lobbyManager.TryJoinLobby(conn, message);
            
            conn.Send(new JoinLobbyResultMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
                LobbyId = message.LobbyId
            });
        }

        private void OnCreateLobbyRequested(NetworkConnectionToClient conn, CreateLobbyMessage message)
        {
            var result = _lobbyManager.TryCreateLobby(conn, message);
            
            conn.Send(new CreateLobbyResultMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
                LobbyId = result == LobbyError.None ? _lobbyManager.GetLobbyId(conn) : null
            });
            
        }


        public void Dispose()
        {
            
        }
    }
}