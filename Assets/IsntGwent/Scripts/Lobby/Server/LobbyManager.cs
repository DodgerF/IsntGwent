using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Network;
using IsntGwent.Scripts.Match.Server;
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
        [Inject] private readonly DeckValidator _deckValidator;
        [Inject] private readonly CardDatabase _cardDatabase;
        [Inject] private readonly DeckRulesProvider _deckRules;
        [Inject] private readonly SeatRegistry _seats;

        public static readonly TimeSpan MatchGracePeriod = TimeSpan.FromSeconds(90);
        public static readonly TimeSpan LobbyGracePeriod = TimeSpan.FromSeconds(180);

        private static readonly DeckViolation[] NoViolations = new DeckViolation[0];

        private readonly Dictionary<string, LobbyRoom> _rooms = new();
        private readonly Dictionary<Seat, string> _seatLobbyMap = new();
        private readonly Dictionary<string, GameContext> _games = new();
        private readonly Dictionary<string, string> _tokenLobbyMap = new();
        private readonly Dictionary<string, IDisposable> _graceTimers = new();
        private readonly HashSet<Seat> _pendingResume = new();

        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            MyNetManager.ServerDisconnected
                .Subscribe(OnDisconnect)
                .AddTo(_disposables);

            _serverHandler.OnCreateLobby
                .Subscribe(t => OnCreateLobbyRequested(t.conn, t.msg))
                .AddTo(_disposables);
            _serverHandler.OnJoinLobby
                .Subscribe(t => OnJoinLobbyRequested(t.conn, t.msg))
                .AddTo(_disposables);
            _serverHandler.OnReady
                .Subscribe(SetReady)
                .AddTo(_disposables);
            _serverHandler.OnReconnect
                .Subscribe(t => OnReconnectRequested(t.conn, t.msg))
                .AddTo(_disposables);
        }

        private void OnCreateLobbyRequested(NetworkConnectionToClient conn, CreateLobbyMessage msg)
        {
            var result = TryCreateLobby(new MirrorSeatChannel(conn), _seats.Resolve(conn), msg,
                out var seat, out var violations);

            conn.Send(new CreateLobbyResultMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
                Violations = violations,
                SeatToken = seat != null ? seat.Token : string.Empty,
            });
        }

        private void OnJoinLobbyRequested(NetworkConnectionToClient conn, JoinLobbyMessage msg)
        {
            var result = TryJoinLobby(new MirrorSeatChannel(conn), _seats.Resolve(conn), msg,
                out var seat, out var violations);

            conn.Send(new JoinLobbyResultMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
                Violations = violations,
                SeatToken = seat != null ? seat.Token : string.Empty,
            });
        }

        public void SetReady(Seat seat)
        {
            if (seat == null) return;

            if (!_seatLobbyMap.TryGetValue(seat, out var lobbyId)) return;
            if (!_rooms.TryGetValue(lobbyId, out var room)) return;
            if (!room.Contains(seat)) return;

            seat.IsReady = true;

            if (_games.TryGetValue(lobbyId, out var context))
            {
                ResumeAfterReconnect(context, seat);
                return;
            }

            TryStartLobby(lobbyId);
        }

        private void OnReconnectRequested(NetworkConnectionToClient conn, ReconnectRequestMessage msg)
        {
            var seat = _seats.GetByToken(msg.Token);

            if (seat == null || !_tokenLobbyMap.TryGetValue(msg.Token, out var lobbyId))
            {
                conn.Send(new ReconnectResultMessage { IsSuccess = false });
                return;
            }

            if (!_rooms.TryGetValue(lobbyId, out var room) || !room.Contains(seat))
            {
                conn.Send(new ReconnectResultMessage { IsSuccess = false });
                return;
            }

            var context = _games.TryGetValue(lobbyId, out var game) ? game : null;

            if (context != null && context.GameEnded.Value)
            {
                conn.Send(new ReconnectResultMessage { IsSuccess = false });
                return;
            }

            CancelGrace(seat.Token);

            if (context != null && !seat.IsConnected)
                _pendingResume.Add(seat);

            _seats.Attach(seat, conn);
            _seatLobbyMap[seat] = lobbyId;
            seat.IsReady = false;

            conn.Send(new ReconnectResultMessage
            {
                IsSuccess = true,
                Phase = context != null ? ReconnectPhase.Match : ReconnectPhase.Lobby,
            });
        }

        private void ResumeAfterReconnect(GameContext context, Seat seat)
        {
            var player = context.GetPlayer(seat);
            if (player == null) return;

            var wasDisconnected = _pendingResume.Remove(seat);

            _notifier.NotifySnapshot(context, player);

            if (!wasDisconnected) return;
            if (context.IsPaused) return;

            _notifier.NotifyOpponentReconnecting(context.GetOpponent(player), false);
        }

        private void BeginMatchGrace(GameContext context, Player player)
        {
            _notifier.NotifyOpponentReconnecting(context.GetOpponent(player), true);

            BeginGrace(player.Seat, MatchGracePeriod, OnMatchGraceExpired);
        }

        private void BeginLobbyGrace(Seat seat)
        {
            BeginGrace(seat, LobbyGracePeriod, OnLobbyGraceExpired);
        }

        private void BeginGrace(Seat seat, TimeSpan period, Action<string> onExpired)
        {
            if (seat == null) return;

            var token = seat.Token;

            CancelGrace(token);

            _graceTimers[token] = Observable
                .Timer(period)
                .Subscribe(_ => onExpired(token));
        }

        private void OnMatchGraceExpired(string token)
        {
            _graceTimers.Remove(token);

            var seat = _seats.GetByToken(token);
            if (seat == null) return;
            if (!_tokenLobbyMap.TryGetValue(token, out var lobbyId)) return;
            if (!_games.TryGetValue(lobbyId, out var context)) return;

            var player = context.GetPlayer(seat);
            if (player == null) return;

            _gameController.EndGameByDisconnect(context, context.GetOpponent(player));
        }

        private void OnLobbyGraceExpired(string token)
        {
            _graceTimers.Remove(token);

            var seat = _seats.GetByToken(token);
            if (seat == null) return;
            if (!_tokenLobbyMap.TryGetValue(token, out var lobbyId)) return;
            if (_games.ContainsKey(lobbyId)) return;
            if (!_rooms.TryGetValue(lobbyId, out var room)) return;

            NotifyLobbyOpponentLeft(room, seat);
            RemoveFromRoom(lobbyId, room, seat);
        }

        private void CancelGrace(string token)
        {
            if (string.IsNullOrEmpty(token)) return;
            if (!_graceTimers.TryGetValue(token, out var timer)) return;

            timer.Dispose();
            _graceTimers.Remove(token);
        }

        public LobbyError TryCreateLobby(ISeatChannel channel, Seat existing, CreateLobbyMessage msg,
            out Seat seat, out DeckViolation[] violations)
        {
            seat = null;

            if (!IsDeckAcceptable(msg.Deck, out violations))
                return LobbyError.DeckInvalid;

            if (existing != null)
                return LobbyError.AlreadyInLobby;

            var data = new LobbyData
            {
                LobbyId = Guid.NewGuid().ToString(),
                Name = msg.Name,
                IsPrivate = !string.IsNullOrEmpty(msg.Password)
            };
            var room = new LobbyRoom(data, msg.Password);

            seat = _seats.Create(channel, msg.Deck);
            room.TryAddSeat(seat);

            _rooms.Add(room.Data.LobbyId, room);
            _seatLobbyMap[seat] = room.Data.LobbyId;
            _tokenLobbyMap[seat.Token] = room.Data.LobbyId;

            PublishLobby(room);

            return LobbyError.None;
        }

        public LobbyError TryJoinLobby(ISeatChannel channel, Seat existing, JoinLobbyMessage message,
            out Seat seat, out DeckViolation[] violations)
        {
            seat = null;

            if (!IsDeckAcceptable(message.Deck, out violations))
                return LobbyError.DeckInvalid;

            if (!_rooms.TryGetValue(message.LobbyId, out var room))
                return LobbyError.LobbyNotFound;

            if (existing != null)
                return LobbyError.AlreadyInLobby;

            if (room.IsFull)
                return LobbyError.LobbyFull;

            if (room.Data.IsPrivate && message.Password != room.Password)
                return LobbyError.InvalidPassword;

            seat = _seats.Create(channel, message.Deck);
            room.TryAddSeat(seat);

            _seatLobbyMap[seat] = message.LobbyId;
            _tokenLobbyMap[seat.Token] = message.LobbyId;

            PublishLobby(room);

            return LobbyError.None;
        }

        private bool IsDeckAcceptable(DeckDefinition deck, out DeckViolation[] violations)
        {
            violations = NoViolations;

            if (!_cardDatabase.OnLoaded.Value || !_deckRules.OnLoaded.Value)
            {
                Debug.LogWarning("Deck check skipped: card database or deck rules are not loaded yet");
                return true;
            }

            var found = _deckValidator.Validate(deck);
            if (found.Count == 0) return true;

            violations = found.ToArray();
            Debug.Log("Deck rejected: " + DeckViolationCodes.ToWire(violations[0].Code));
            return false;
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
            gc.SetPlayers(room.Seats.First(), room.Seats.Last());
            _games.Add(lobbyId, gc);

            UnpublishLobby(lobbyId);

            gc.GameEnded
                .Where(v => v)
                .Take(1)
                .Subscribe(_ => DropGame(lobbyId))
                .AddTo(_disposables);

            _gameController.StartGame(gc);

            return true;
        }

        public void OnDisconnect(NetworkConnectionToClient conn)
        {
            var seat = _seats.Resolve(conn);
            if (seat == null) return;

            _seats.Detach(conn);

            if (!_seatLobbyMap.TryGetValue(seat, out var lobbyId)) return;

            if (_games.TryGetValue(lobbyId, out var context))
            {
                var leavingPlayer = context.GetPlayer(seat);
                if (leavingPlayer != null && !context.GameEnded.Value)
                {
                    BeginMatchGrace(context, leavingPlayer);
                    return;
                }
            }

            if (!_rooms.ContainsKey(lobbyId)) return;

            seat.IsReady = false;

            BeginLobbyGrace(seat);
        }

        public void LeaveLobby(Seat seat)
        {
            if (seat == null) return;
            if (!_seatLobbyMap.TryGetValue(seat, out var lobbyId)) return;

            if (!_rooms.TryGetValue(lobbyId, out var room))
            {
                ReleaseSeat(seat);
                return;
            }

            NotifyLobbyOpponentLeft(room, seat);
            RemoveFromRoom(lobbyId, room, seat);
        }

        private void NotifyLobbyOpponentLeft(LobbyRoom room, Seat seat)
        {
            if (_games.ContainsKey(room.Data.LobbyId)) return;

            var opponent = room.GetOpponent(seat);
            if (opponent is not { IsReady: true }) return;

            opponent.Send(new EnemyDisconnectedMessage());
        }

        private void RemoveFromRoom(string lobbyId, LobbyRoom room, Seat seat)
        {
            room.RemoveSeat(seat);
            ReleaseSeat(seat);

            if (room.Seats.Count != 0)
            {
                PublishLobby(room);
                return;
            }

            _rooms.Remove(lobbyId);
            UnpublishLobby(lobbyId);
        }

        private void ReleaseSeat(Seat seat)
        {
            if (seat == null) return;

            CancelGrace(seat.Token);
            _pendingResume.Remove(seat);
            _seatLobbyMap.Remove(seat);
            _tokenLobbyMap.Remove(seat.Token);
            _seats.Release(seat);
        }

        public int LobbiesVersion { get; private set; }

        public IReadOnlyList<LobbyData> Lobbies => _hub.SyncLobbies;

        public string GetLobbyId(Seat seat)
        {
            if (seat == null) return null;

            return _seatLobbyMap.TryGetValue(seat, out var lobbyId) ? lobbyId : null;
        }

        private void PublishLobby(LobbyRoom room)
        {
            LobbiesVersion++;

            var data = new LobbyData
            {
                LobbyId = room.Data.LobbyId,
                Name = room.Data.Name,
                IsPrivate = room.Data.IsPrivate,
                Players = room.Seats.Count,
                MaxPlayers = LobbyRoom.MaxPlayers,
            };

            var index = IndexOfLobby(room.Data.LobbyId);

            if (index < 0)
                _hub.SyncLobbies.Add(data);
            else
                _hub.SyncLobbies[index] = data;
        }

        private void UnpublishLobby(string lobbyId)
        {
            var index = IndexOfLobby(lobbyId);
            if (index < 0) return;

            LobbiesVersion++;
            _hub.SyncLobbies.RemoveAt(index);
        }

        private int IndexOfLobby(string lobbyId)
        {
            for (var i = 0; i < _hub.SyncLobbies.Count; i++)
            {
                if (_hub.SyncLobbies[i].LobbyId == lobbyId)
                    return i;
            }

            return -1;
        }

        private void DropGame(string lobbyId)
        {
            if (!_games.TryGetValue(lobbyId, out var context)) return;

            ReleaseSeat(context.Player1.Seat);
            ReleaseSeat(context.Player2.Seat);

            if (_rooms.TryGetValue(lobbyId, out var room))
            {
                _rooms.Remove(lobbyId);
                UnpublishLobby(lobbyId);
            }

            _games.Remove(lobbyId);
            context.Dispose();
        }

        public GameContext GetGameContext(Seat seat)
        {
            if (seat == null) return null;
            if (!_seatLobbyMap.TryGetValue(seat, out var lobbyId)) return null;

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
            _seatLobbyMap.Clear();
            _pendingResume.Clear();
            _seats.Clear();

            _disposables.Dispose();
        }
    }
}
