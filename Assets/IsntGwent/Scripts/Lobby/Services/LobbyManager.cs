using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Network;
using IsntGwent.Scripts.Messages;
using Mirror;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Lobby.Services
{
    public class LobbyManager
    {
        [Inject] private readonly LobbyNetworkHub _hub;

        private readonly Dictionary<string, LobbyRoom> _rooms = new();
        private readonly Dictionary<NetworkConnectionToClient, string> _playerLobbyMap = new();
        
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
            room.TryAddPlayer(conn);
            
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
            
            room.TryAddPlayer(conn);
            _playerLobbyMap[conn] = message.LobbyId;

            if (room.IsFull)
                StartLobby(room);
            
            
            return LobbyError.None;
        }

        public string GetLobbyId(NetworkConnectionToClient conn)
        {
            return _playerLobbyMap[conn];
        }

        private void StartLobby(LobbyRoom room)
        {
            _hub.SyncLobbies.Remove(room.Data);
            
            foreach (var player in room.Players)
            {
                //player.Send(new GameStartedMessage());
            }
        }
    }
}