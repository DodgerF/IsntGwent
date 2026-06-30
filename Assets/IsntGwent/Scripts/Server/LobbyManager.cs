using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Network;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Server
{
    public class LobbyManager : IDisposable
    {
        [Inject] private readonly LobbyNetworkHub _hub;
        [Inject] private readonly DiContainer _container;

        private readonly Dictionary<string, LobbyRoom> _rooms = new();
        private readonly Dictionary<NetworkConnectionToClient, string> _playerLobbyMap = new();
        private readonly Dictionary<string, GameContext> _games = new();
        
        private readonly CompositeDisposable _disposables = new();
        
        public LobbyError TryCreateLobby(NetworkConnectionToClient conn, CreateLobbyMessage msg)
        {
            if (_playerLobbyMap.ContainsKey(conn))
                return LobbyError.AlreadyInLobby;
            
            var data = new LobbyData
            {
                LobbyId = Guid.NewGuid().ToString(),
                Name = msg.Name,
                IsPrivate = !string.IsNullOrEmpty(msg.Password)
            };
            var room = new LobbyRoom(data, msg.Password);
            room.TryAddPlayer(new PlayerLobby(conn, msg.Deck));
            
            _rooms.Add(room.Data.LobbyId, room);
            _playerLobbyMap[conn] = room.Data.LobbyId;
            _hub.SyncLobbies.Add(room.Data);
            
            return LobbyError.None;
        }
        
        public LobbyError TryJoinLobby(NetworkConnectionToClient conn, JoinLobbyMessage message)
        {
            if (!_rooms.TryGetValue(message.LobbyId, out var room))
                return LobbyError.LobbyNotFound;
                
            
            if (_playerLobbyMap.ContainsKey(conn))
                return LobbyError.AlreadyInLobby;
            
            if (room.IsFull)
                return LobbyError.LobbyFull;
            
            if (room.Data.IsPrivate && message.Password != room.Password)
            {
                Debug.Log("invalid password");
                return LobbyError.InvalidPassword;
            }
            
            room.TryAddPlayer(new PlayerLobby(conn, message.Deck));
            _playerLobbyMap[conn] = message.LobbyId;
            
            
            return LobbyError.None;
        }

        public string GetLobbyId(NetworkConnectionToClient conn)
        {
            return _playerLobbyMap[conn];
        }

        public bool TryStartLobby(string lobbyId)
        {
            var room = _rooms[lobbyId];
            if (!room.IsFull)
                return false;
            
            var gc = _container.Instantiate<GameContext>();
            gc.SetPlayers(room.Players.First(), room.Players.Last());
            _games.Add(lobbyId, gc);
            _hub.SyncLobbies.Remove(room.Data);

            gc.GameEnded
                .Subscribe(_ =>
                {
                    _games.Remove(lobbyId);
                })
                .AddTo(_disposables);
            
            GameControllerServer.StartGame(gc); 

            return true;
        }
        
        public GameContext GetGameContext(NetworkConnectionToClient conn)
        {
            if (!_playerLobbyMap.TryGetValue(conn, out var lobbyId)) return null;
            _games.TryGetValue(lobbyId, out var context);
            return context;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}