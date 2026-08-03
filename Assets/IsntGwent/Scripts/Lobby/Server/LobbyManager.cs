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
        [Inject] private readonly MatchServerNotifier _notifier;

        public static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(45);

        private readonly Dictionary<string, LobbyRoom> _rooms = new();
        private readonly Dictionary<NetworkConnectionToClient, string> _playerLobbyMap = new();
        private readonly Dictionary<string, GameContext> _games = new();
        private readonly Dictionary<string, string> _tokenLobbyMap = new();
        private readonly Dictionary<string, IDisposable> _graceTimers = new();

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
            _serverHandler.OnReconnect
                .Subscribe(t => OnReconnectRequested(t.conn, t.msg))
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
            if (!_playerLobbyMap.TryGetValue(conn, out var lobbyId)) return;
            if (!_rooms.TryGetValue(lobbyId, out var room)) return;

            room.SetReady(conn);

            if (_games.TryGetValue(lobbyId, out var context))
            {
                ResumeAfterReconnect(context, conn);
                return;
            }

            TryStartLobby(lobbyId);
        }

        private void OnReconnectRequested(NetworkConnectionToClient conn, ReconnectRequestMessage msg)
        {
            var player = FindPlayerByToken(msg.Token, out var lobbyId, out var context);

            if (player == null || context.GameEnded.Value)
            {
                conn.Send(new ReconnectResultMessage { IsSuccess = false });
                return;
            }

            CancelGrace(msg.Token);

            var oldConn = player.Connection;

            if (oldConn != null)
                _playerLobbyMap.Remove(oldConn);

            player.Connection = conn;
            _playerLobbyMap[conn] = lobbyId;

            if (_rooms.TryGetValue(lobbyId, out var room))
            {
                var seat = room.GetPlayer(oldConn);
                if (seat != null)
                {
                    seat.Connection = conn;
                    seat.IsReady = false;
                }
            }

            conn.Send(new ReconnectResultMessage { IsSuccess = true });
        }

        private Player FindPlayerByToken(string token, out string lobbyId, out GameContext context)
        {
            lobbyId = null;
            context = null;

            if (string.IsNullOrEmpty(token)) return null;
            if (!_tokenLobbyMap.TryGetValue(token, out lobbyId)) return null;
            if (!_games.TryGetValue(lobbyId, out context)) return null;

            return context.GetPlayerByToken(token);
        }

        private void ResumeAfterReconnect(GameContext context, NetworkConnectionToClient conn)
        {
            var player = context.GetPlayer(conn);
            if (player == null) return;

            var wasDisconnected = !player.IsConnected;
            player.IsConnected = true;

            _notifier.NotifySnapshot(context, player);

            if (!wasDisconnected) return;
            if (context.IsPaused) return;

            _notifier.NotifyOpponentReconnecting(context.GetOpponent(player), false);
        }

        private void BeginGrace(GameContext context, Player player)
        {
            player.IsConnected = false;

            _notifier.NotifyOpponentReconnecting(context.GetOpponent(player), true);

            var token = player.ReconnectToken;

            CancelGrace(token);

            _graceTimers[token] = Observable
                .Timer(GracePeriod)
                .Subscribe(_ => OnGraceExpired(token));
        }

        private void OnGraceExpired(string token)
        {
            _graceTimers.Remove(token);

            var player = FindPlayerByToken(token, out var lobbyId, out var context);
            if (player == null) return;

            _gameController.EndGameByDisconnect(context, context.GetOpponent(player));

            if (!_rooms.TryGetValue(lobbyId, out var room)) return;

            RemoveFromRoom(lobbyId, room, player.Connection);
        }

        private void CancelGrace(string token)
        {
            if (string.IsNullOrEmpty(token)) return;
            if (!_graceTimers.TryGetValue(token, out var timer)) return;

            timer.Dispose();
            _graceTimers.Remove(token);
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

        public bool TryStartLobby(string lobbyId)
        {
            if (!_rooms.TryGetValue(lobbyId, out var room))
                return false;

            if (_games.ContainsKey(lobbyId))
                return false;

            if (!room.IsFull || !room.AllReady)
                return false;

            var gc = _container.Instantiate<GameContext>();
            gc.SetPlayers(room.Players.First(), room.Players.Last());
            _games.Add(lobbyId, gc);
            _tokenLobbyMap[gc.Player1.ReconnectToken] = lobbyId;
            _tokenLobbyMap[gc.Player2.ReconnectToken] = lobbyId;
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
                if (leavingPlayer != null && !context.GameEnded.Value)
                {
                    BeginGrace(context, leavingPlayer);
                    return;
                }
            }

            if (!_rooms.TryGetValue(lobbyId, out var room)) return;

            NotifyLobbyOpponentLeft(room, conn);
            RemoveFromRoom(lobbyId, room, conn);
        }

        public void LeaveLobby(NetworkConnectionToClient conn)
        {
            if (!_playerLobbyMap.TryGetValue(conn, out var lobbyId)) return;
            _playerLobbyMap.Remove(conn);

            if (!_rooms.TryGetValue(lobbyId, out var room)) return;

            NotifyLobbyOpponentLeft(room, conn);
            RemoveFromRoom(lobbyId, room, conn);
        }

        private void NotifyLobbyOpponentLeft(LobbyRoom room, NetworkConnectionToClient conn)
        {
            if (_games.ContainsKey(room.Data.LobbyId)) return;

            var opponent = room.GetOpponent(conn);
            if (opponent is not { IsReady: true }) return;
            if (opponent.Connection == null || !opponent.Connection.isReady) return;

            opponent.Connection.Send(new EnemyDisconnectedMessage());
        }

        private void RemoveFromRoom(string lobbyId, LobbyRoom room, NetworkConnectionToClient conn)
        {
            room.RemovePlayer(conn);

            if (room.Players.Count != 0) return;

            _rooms.Remove(lobbyId);
            _hub.SyncLobbies.Remove(room.Data);
        }

        private void DropGame(string lobbyId)
        {
            if (!_games.TryGetValue(lobbyId, out var context)) return;

            CancelGrace(context.Player1.ReconnectToken);
            CancelGrace(context.Player2.ReconnectToken);

            _tokenLobbyMap.Remove(context.Player1.ReconnectToken);
            _tokenLobbyMap.Remove(context.Player2.ReconnectToken);

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
            foreach (var timer in _graceTimers.Values)
                timer.Dispose();
            _graceTimers.Clear();

            foreach (var context in _games.Values)
                context.Dispose();
            _games.Clear();
            _tokenLobbyMap.Clear();

            _disposables.Dispose();
        }
    }
}