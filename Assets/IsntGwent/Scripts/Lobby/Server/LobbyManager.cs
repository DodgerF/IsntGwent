using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Network;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Match.Client;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using Mirror;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Lobby.Server
{
    public class LobbyManager : IInitializable, IDisposable
    {
        [Inject] private readonly LobbyNetworkHub _hub;
        [Inject] private readonly DiContainer _container;
        [Inject] private readonly GameControllerServer _gameController;
        [Inject] private readonly ServerHandler _serverHandler;

        private readonly Dictionary<string, LobbyRoom> _rooms = new();
        private readonly Dictionary<NetworkConnectionToClient, string> _playerLobbyMap = new();
        private readonly Dictionary<string, GameContext> _games = new();
        
        private readonly CompositeDisposable _disposables = new();
        
        public void Initialize()
        {
            if (!NetworkServer.active) return;

            MyNetManager.ServerDisconnected
                .Subscribe(conn =>
                {
                    OnDisconnect(conn);
                })
                .AddTo(_disposables);

            _serverHandler.OnCreateLobby
                .Subscribe(t => OnCreateLobbyRequested(t.conn, t.msg))
                .AddTo(_disposables);
            _serverHandler.OnJoinLobby
                .Subscribe(t => OnJoinLobbyRequested(t.conn, t.msg))
                .AddTo(_disposables);
            _serverHandler.OnReady
                .Subscribe(OnReady)
                .AddTo(_disposables);
        }

        private void OnCreateLobbyRequested(NetworkConnectionToClient conn, CreateLobbyMessage msg)
        {
            var result = TryCreateLobby(conn, msg);
            conn.Send(new CreateLobbyResultMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
            });
        }

        private void OnJoinLobbyRequested(NetworkConnectionToClient conn, JoinLobbyMessage msg)
        {
            var result = TryJoinLobby(conn, msg);
            conn.Send(new JoinLobbyResultMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
            });
        }

        private void OnReady(NetworkConnectionToClient conn)
        {
            var lobbyId = GetLobbyId(conn);
            TryStartLobby(lobbyId);
        }
        
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
                .Where(v=> v)
                .Take(1)
                .Subscribe(_ => DropGame(lobbyId))
                .AddTo(_disposables);

            _gameController.StartGame(gc);

            return true;
        }

        public void OnDisconnect(NetworkConnectionToClient conn)
        {
            if (!_playerLobbyMap.TryGetValue(conn, out var lobbyId)) return;
            _playerLobbyMap.Remove(conn);
            
            if (_games.TryGetValue(lobbyId, out var context))
            {
                var leavingPlayer = context.GetPlayer(conn);
                if (leavingPlayer != null)
                {
                    var winner = context.GetOpponent(leavingPlayer);
                    _gameController.EndGameByDisconnect(context, winner);
                }
            }
            
            if (!_rooms.TryGetValue(lobbyId, out var room)) return;

            room.RemovePlayer(conn);

            if (room.Players.Count == 0)
            {
                _rooms.Remove(lobbyId);
                _hub.SyncLobbies.Remove(room.Data);
            }
        }
        
        public void LeaveLobby(NetworkConnectionToClient conn)
        {
            if (!_playerLobbyMap.TryGetValue(conn, out var lobbyId)) return;
            _playerLobbyMap.Remove(conn);
            
            if (!_rooms.TryGetValue(lobbyId, out var room)) return;
            room.RemovePlayer(conn);

            if (room.Players.Count == 0)
            {
                _rooms.Remove(lobbyId);
                _hub.SyncLobbies.Remove(room.Data);
            }
        }
        
        private void DropGame(string lobbyId)
        {
            if (!_games.TryGetValue(lobbyId, out var context)) return;

            _games.Remove(lobbyId);
            context.Dispose();
        }

        public GameContext GetGameContext(NetworkConnectionToClient conn)
        {
            if (!_playerLobbyMap.TryGetValue(conn, out var lobbyId)) return null;
            _games.TryGetValue(lobbyId, out var context);
            return context;
        }

        public void Dispose()
        {
            foreach (var context in _games.Values)
                context.Dispose();
            _games.Clear();

            _disposables.Dispose();
        }
    }
}