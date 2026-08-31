using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Accounts.Server;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Match.Server.Journal;
using IsntGwent.Scripts.Match.Server.Stats;
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
        [Inject] private readonly DiContainer _container;
        [Inject] private readonly GameControllerServer _gameController;
        [Inject] private readonly ServerHandler _serverHandler;
        [Inject] private readonly MatchServerNotifier _notifier;
        [Inject] private readonly DeckValidator _deckValidator;
        [Inject] private readonly CardDatabase _cardDatabase;
        [Inject] private readonly DeckRulesProvider _deckRules;
        [Inject] private readonly SeatRegistry _seats;
        [Inject] private readonly AccountRegistry _accounts;
        [Inject] private readonly MatchmakingQueue _queue;
        [Inject] private readonly MatchStatsRecorder _stats;
        [Inject] private readonly MatchJournalFactory _journals;
        [Inject] private readonly CardStatsStore _cardStats;

        public static readonly TimeSpan MatchGracePeriod = TimeSpan.FromSeconds(90);
        public static readonly TimeSpan LobbyGracePeriod = TimeSpan.FromSeconds(180);
        public static readonly TimeSpan PrivateRoomTtl = TimeSpan.FromMinutes(10);

        private static readonly DeckViolation[] NoViolations = new DeckViolation[0];

        private readonly Dictionary<string, LobbyRoom> _rooms = new();
        private readonly Dictionary<Seat, string> _seatLobbyMap = new();
        private readonly Dictionary<string, GameContext> _games = new();
        private readonly Dictionary<string, MatchJournal> _journalsByLobby = new();
        private readonly Dictionary<string, string> _tokenLobbyMap = new();
        private readonly Dictionary<string, IDisposable> _graceTimers = new();
        private readonly Dictionary<string, IDisposable> _roomTimers = new();
        private readonly HashSet<Seat> _pendingResume = new();

        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            MyNetManager.ServerDisconnected
                .Subscribe(OnDisconnect)
                .AddTo(_disposables);

            _serverHandler.OnFindMatch
                .Subscribe(t => OnFindMatchRequested(t.conn, t.msg))
                .AddTo(_disposables);
            _serverHandler.OnCancelSearch
                .Subscribe(OnCancelSearchRequested)
                .AddTo(_disposables);
            _serverHandler.OnCreatePrivateRoom
                .Subscribe(t => OnCreatePrivateRoomRequested(t.conn, t.msg))
                .AddTo(_disposables);
            _serverHandler.OnJoinByCode
                .Subscribe(t => OnJoinByCodeRequested(t.conn, t.msg))
                .AddTo(_disposables);
            _serverHandler.OnReady
                .Subscribe(SetReady)
                .AddTo(_disposables);
            _serverHandler.OnReconnect
                .Subscribe(t => OnReconnectRequested(t.conn, t.msg))
                .AddTo(_disposables);
        }

        private void OnFindMatchRequested(NetworkConnectionToClient conn, FindMatchMessage msg)
        {
            var result = TryFindMatch(conn, msg.Deck, out var violations);

            conn.Send(new SearchStartedMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
                Violations = violations,
            });

            if (result != LobbyError.None) return;

            TryPair();
        }

        private LobbyError TryFindMatch(NetworkConnectionToClient conn, DeckDefinition deck,
            out DeckViolation[] violations)
        {
            if (!IsDeckAcceptable(deck, out violations))
                return LobbyError.DeckInvalid;

            var account = _accounts.Resolve(conn);
            if (account == null)
                return LobbyError.NotLoggedIn;

            if (_seats.Resolve(conn) != null)
                return LobbyError.AlreadyInLobby;

            var seat = _seats.Create(new MirrorSeatChannel(conn), deck, account);
            _queue.Enqueue(seat);

            return LobbyError.None;
        }

        private void TryPair()
        {
            if (_queue.Count < LobbyRoom.MaxPlayers) return;

            if (!_queue.TryDequeue(out var first)) return;

            if (!_queue.TryDequeue(out var second))
            {
                _queue.Enqueue(first);
                return;
            }

            var room = CreateRoom(true, string.Empty);

            PlaceSeat(room, first);
            PlaceSeat(room, second);

            first.Send(new MatchFoundMessage { SeatToken = first.Token });
            second.Send(new MatchFoundMessage { SeatToken = second.Token });
        }

        private void OnCancelSearchRequested(NetworkConnectionToClient conn)
        {
            var seat = _seats.Resolve(conn);
            if (seat == null) return;

            if (!_queue.Remove(seat)) return;

            ReleaseSeat(seat);
        }

        private void OnCreatePrivateRoomRequested(NetworkConnectionToClient conn, CreatePrivateRoomMessage msg)
        {
            var result = TryCreatePrivateRoom(conn, msg.Deck, out var seat, out var code, out var violations);

            conn.Send(new PrivateRoomCreatedMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
                Violations = violations,
                SeatToken = seat != null ? seat.Token : string.Empty,
                JoinCode = code,
            });
        }

        private LobbyError TryCreatePrivateRoom(NetworkConnectionToClient conn, DeckDefinition deck,
            out Seat seat, out string code, out DeckViolation[] violations)
        {
            seat = null;
            code = string.Empty;

            if (!IsDeckAcceptable(deck, out violations))
                return LobbyError.DeckInvalid;

            var account = _accounts.Resolve(conn);
            if (account == null)
                return LobbyError.NotLoggedIn;

            if (_seats.Resolve(conn) != null)
                return LobbyError.AlreadyInLobby;

            code = JoinCodes.Generate(IsCodeTaken);

            var room = CreateRoom(false, code);

            seat = _seats.Create(new MirrorSeatChannel(conn), deck, account);
            PlaceSeat(room, seat);

            BeginRoomTtl(room);

            return LobbyError.None;
        }

        private void OnJoinByCodeRequested(NetworkConnectionToClient conn, JoinByCodeMessage msg)
        {
            var result = TryJoinByCode(conn, msg.Code, msg.Deck, out var seat, out var violations);

            conn.Send(new JoinByCodeResultMessage
            {
                IsSuccess = result == LobbyError.None,
                Error = result,
                Violations = violations,
                SeatToken = seat != null ? seat.Token : string.Empty,
            });
        }

        private LobbyError TryJoinByCode(NetworkConnectionToClient conn, string code, DeckDefinition deck,
            out Seat seat, out DeckViolation[] violations)
        {
            seat = null;

            if (!IsDeckAcceptable(deck, out violations))
                return LobbyError.DeckInvalid;

            var account = _accounts.Resolve(conn);
            if (account == null)
                return LobbyError.NotLoggedIn;

            if (!JoinCodes.IsWellFormed(code))
                return LobbyError.InvalidCode;

            var room = FindByCode(JoinCodes.Normalize(code));
            if (room == null)
                return LobbyError.RoomNotFound;

            if (_seats.Resolve(conn) != null)
                return LobbyError.AlreadyInLobby;

            if (room.IsFull)
                return LobbyError.RoomFull;

            seat = _seats.Create(new MirrorSeatChannel(conn), deck, account);
            PlaceSeat(room, seat);

            CancelRoomTtl(room.Id);

            var host = room.GetOpponent(seat);
            if (host != null)
                host.Send(new MatchFoundMessage { SeatToken = host.Token });

            return LobbyError.None;
        }

        private LobbyRoom CreateRoom(bool isRanked, string joinCode)
        {
            var room = new LobbyRoom(Guid.NewGuid().ToString(), isRanked, joinCode);

            _rooms.Add(room.Id, room);

            return room;
        }

        private void PlaceSeat(LobbyRoom room, Seat seat)
        {
            room.TryAddSeat(seat);

            _seatLobbyMap[seat] = room.Id;
            _tokenLobbyMap[seat.Token] = room.Id;
        }

        private LobbyRoom FindByCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;

            foreach (var room in _rooms.Values)
            {
                if (room.JoinCode == code) return room;
            }

            return null;
        }

        private bool IsCodeTaken(string code)
        {
            return FindByCode(code) != null;
        }

        private void BeginRoomTtl(LobbyRoom room)
        {
            CancelRoomTtl(room.Id);

            var roomId = room.Id;

            _roomTimers[roomId] = Observable
                .Timer(PrivateRoomTtl)
                .Subscribe(_ => OnRoomTtlExpired(roomId));
        }

        private void CancelRoomTtl(string roomId)
        {
            if (string.IsNullOrEmpty(roomId)) return;
            if (!_roomTimers.TryGetValue(roomId, out var timer)) return;

            timer.Dispose();
            _roomTimers.Remove(roomId);
        }

        private void OnRoomTtlExpired(string roomId)
        {
            _roomTimers.Remove(roomId);

            if (_games.ContainsKey(roomId)) return;
            if (!_rooms.TryGetValue(roomId, out var room)) return;
            if (room.IsFull) return;

            foreach (var seat in room.Seats.ToArray())
            {
                room.RemoveSeat(seat);
                ReleaseSeat(seat);
            }

            _rooms.Remove(roomId);
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

            Log.Info(LogTag.Net, $"{Who(player)} resumed match {context.MatchId}");
            context.Journal?.Resume(player);
            if (context.IsPaused) return;

            _notifier.NotifyOpponentReconnecting(context.GetOpponent(player), false);
        }

        private void BeginMatchGrace(GameContext context, Player player)
        {
            Log.Info(LogTag.Net, $"{Who(player)} lost connection, match {context.MatchId} paused");
            context.Journal?.Pause(player);

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

        private bool IsDeckAcceptable(DeckDefinition deck, out DeckViolation[] violations)
        {
            violations = NoViolations;

            if (!_cardDatabase.OnLoaded.Value || !_deckRules.OnLoaded.Value)
            {
                Log.Warn(LogTag.Lobby, "Deck check skipped: card database or deck rules are not loaded yet");
                return true;
            }

            var found = _deckValidator.Validate(deck);
            if (found.Count == 0) return true;

            violations = found.ToArray();
            Log.Info(LogTag.Lobby, "Deck rejected: " + DeckViolationCodes.ToWire(violations[0].Code));
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

            CancelRoomTtl(lobbyId);

            var gc = _container.Instantiate<GameContext>();
            gc.SetPlayers(room.Seats.First(), room.Seats.Last());
            gc.IsRanked = room.IsRanked;
            _games.Add(lobbyId, gc);

            var journal = _journals.Create(gc, lobbyId);
            if (journal != null) _journalsByLobby[lobbyId] = journal;

            Log.Info(LogTag.Lobby,
                $"match started: {Who(gc.Player1)} vs {Who(gc.Player2)} " +
                $"({(gc.IsRanked ? "ranked" : "private")}, match {gc.MatchId}, lobby {lobbyId})");

            gc.GameEnded
                .Where(v => v)
                .Take(1)
                .Subscribe(_ =>
                {
                    _stats.Record(gc);

                    Log.Info(LogTag.Lobby,
                        $"match ended ({gc.EndReason}): " +
                        $"{(gc.IsTie ? "tie" : Who(gc.Winner) + " wins")} (match {gc.MatchId})");

                    gc.Journal?.End(gc, gc.EndReason);

                    if (gc.Journal != null)
                        _cardStats.Apply(gc, gc.Journal.Tally);

                    DropGame(lobbyId);
                })
                .AddTo(_disposables);

            _gameController.StartGame(gc);

            return true;
        }

        public void OnDisconnect(NetworkConnectionToClient conn)
        {
            var seat = _seats.Resolve(conn);
            if (seat == null) return;

            _seats.Detach(conn);

            if (_queue.Remove(seat))
            {
                ReleaseSeat(seat);
                return;
            }

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

            if (_queue.Remove(seat))
            {
                ReleaseSeat(seat);
                return;
            }

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
            if (_games.ContainsKey(room.Id)) return;

            var opponent = room.GetOpponent(seat);
            if (opponent is not { IsReady: true }) return;

            opponent.Send(new EnemyDisconnectedMessage());
        }

        private void RemoveFromRoom(string lobbyId, LobbyRoom room, Seat seat)
        {
            room.RemoveSeat(seat);
            ReleaseSeat(seat);

            if (room.Seats.Count != 0)
                return;

            _rooms.Remove(lobbyId);
            CancelRoomTtl(lobbyId);
        }

        private void ReleaseSeat(Seat seat)
        {
            if (seat == null) return;

            CancelGrace(seat.Token);
            _queue.Remove(seat);
            _pendingResume.Remove(seat);
            _seatLobbyMap.Remove(seat);
            _tokenLobbyMap.Remove(seat.Token);
            _seats.Release(seat);
        }

        public string GetLobbyId(Seat seat)
        {
            if (seat == null) return null;

            return _seatLobbyMap.TryGetValue(seat, out var lobbyId) ? lobbyId : null;
        }

        private static string Who(Player player)
        {
            return player?.Seat?.Account?.Nickname ?? "?";
        }

        private void DropGame(string lobbyId)
        {
            if (!_games.TryGetValue(lobbyId, out var context)) return;

            if (_journalsByLobby.TryGetValue(lobbyId, out var journal))
            {
                journal.Dispose();
                _journalsByLobby.Remove(lobbyId);
            }

            context.Journal = null;

            ReleaseSeat(context.Player1.Seat);
            ReleaseSeat(context.Player2.Seat);

            if (_rooms.ContainsKey(lobbyId))
            {
                _rooms.Remove(lobbyId);
                CancelRoomTtl(lobbyId);
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

            foreach (var timer in _roomTimers.Values)
                timer.Dispose();
            _roomTimers.Clear();

            foreach (var journal in _journalsByLobby.Values)
                journal.Dispose();
            _journalsByLobby.Clear();

            foreach (var context in _games.Values)
                context.Dispose();
            _games.Clear();
            _tokenLobbyMap.Clear();
            _seatLobbyMap.Clear();
            _pendingResume.Clear();
            _queue.Clear();
            _seats.Clear();

            _disposables.Dispose();
        }
    }
}
