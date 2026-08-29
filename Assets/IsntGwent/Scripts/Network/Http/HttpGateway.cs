using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Content;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Server;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Messages;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Network.Http
{
    public class HttpGateway : MonoBehaviour
    {
        public const int DefaultPort = 80;
        private const int SessionLifetimeHours = 12;

        private static readonly JsonSerializerSettings WireSettings = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            Converters = { new StringEnumConverter(new CamelCaseNamingStrategy()) },
            NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.None,
        };

        [Inject] private readonly LobbyManager _lobbies;
        [Inject] private readonly GameControllerServer _game;
        [Inject] private readonly MatchIntentService _intents;
        [Inject] private readonly ContentService _content;

        private readonly ConcurrentQueue<GatewayRequest> _incoming = new();
        private readonly List<GatewayRequest> _waiting = new();
        private readonly Dictionary<string, GatewaySession> _sessions = new();

        private HttpListener _listener;

        private void Start()
        {
            if (!NetworkServer.active)
            {
                enabled = false;
                return;
            }

            var port = PortFromCommandLine();

            if (IsTransportPort(port))
            {
                Debug.LogError($"HTTP gateway port {port} is taken by the Mirror transport: " +
                               "the gateway would steal game connections. Set another -httpPort " +
                               "or move the transport port.");
                enabled = false;
                return;
            }

            if (!TryListen($"http://+:{port}/") && !TryListen($"http://localhost:{port}/"))
            {
                enabled = false;
                return;
            }

            Debug.Log($"HTTP gateway is listening on port {port}");

            _ = AcceptLoop();
        }

        private bool TryListen(string prefix)
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(prefix);
                _listener.Start();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"HTTP gateway cannot listen on {prefix}: {e.Message}");
                _listener = null;
                return false;
            }
        }

        private static bool IsTransportPort(int port)
        {
            return Transport.active is TelepathyTransport telepathy && telepathy.port == port;
        }

        private static int PortFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length - 1; i++)
            {
                if (args[i] != "-httpPort") continue;
                if (int.TryParse(args[i + 1], out var port) && port > 0) return port;
            }

            return DefaultPort;
        }

        private async Task AcceptLoop()
        {
            while (_listener is { IsListening: true })
            {
                HttpListenerContext context;

                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception)
                {
                    return;
                }

                _ = HandleAsync(context);
            }
        }

        private async Task HandleAsync(HttpListenerContext context)
        {
            GatewayRequest request;

            try
            {
                request = ReadRequest(context);
            }
            catch (Exception e)
            {
                WriteSafely(context, 400, Json(new ErrorWire { Code = "BAD_REQUEST", Message = e.Message }));
                return;
            }

            _incoming.Enqueue(request);

            var result = await request.Completion.Task;

            WriteSafely(context, result.Status, result.Body, result.ContentType);
        }

        private static GatewayRequest ReadRequest(HttpListenerContext context)
        {
            var body = string.Empty;

            if (context.Request.HasEntityBody)
            {
                using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                body = reader.ReadToEnd();
            }

            var auth = context.Request.Headers["Authorization"] ?? string.Empty;

            return new GatewayRequest
            {
                Method = context.Request.HttpMethod,
                Path = context.Request.Url.AbsolutePath.TrimEnd('/'),
                Query = context.Request.QueryString,
                Body = body,
                SessionId = auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? auth.Substring("Bearer ".Length).Trim()
                    : string.Empty,
            };
        }

        private static void WriteSafely(HttpListenerContext context, int status, byte[] body,
            string contentType = "application/json")
        {
            try
            {
                context.Response.StatusCode = status;

                if (body == null || body.Length == 0)
                {
                    context.Response.ContentLength64 = 0;
                    context.Response.Close();
                    return;
                }

                context.Response.ContentType = contentType;
                context.Response.ContentLength64 = body.Length;
                context.Response.OutputStream.Write(body, 0, body.Length);
                context.Response.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"HTTP gateway failed to answer: {e.Message}");
            }
        }

        private void Update()
        {
            while (_incoming.TryDequeue(out var request))
                Route(request);

            for (var i = _waiting.Count - 1; i >= 0; i--)
            {
                if (!TryFinishPoll(_waiting[i])) continue;

                _waiting.RemoveAt(i);
            }
        }

        private void OnDestroy()
        {
            if (_listener == null) return;

            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch (Exception)
            {
                // сокет уже закрыт — гасить нечего
            }

            _listener = null;
        }

        private void Route(GatewayRequest request)
        {
            var path = request.Path;

            if (request.Method == "GET" && path == "/manifest")
            {
                if (!_content.IsReady)
                {
                    Fail(request, 503, "INTERNAL", "Content is not loaded yet");
                    return;
                }

                Complete(request, 200, Encoding.UTF8.GetBytes(_content.ManifestJson()));
                return;
            }

            if (request.Method == "GET" && path.StartsWith("/content/"))
            {
                Content(request, path.Substring("/content/".Length));
                return;
            }

            if (request.Method == "POST" && path == "/session")
            {
                CreateSession(request);
                return;
            }

            if (!_sessions.TryGetValue(request.SessionId, out var session))
            {
                Fail(request, 401, "UNAUTHORIZED", "Unknown session");
                return;
            }

            if (session.ExpiresAt <= Now())
            {
                _sessions.Remove(session.SessionId);
                Fail(request, 401, "SESSION_EXPIRED", "Session expired");
                return;
            }

            switch (request.Method)
            {
                case "GET" when path == "/session/state":
                    SessionState(request, session);
                    return;

                case "GET" when path == "/lobbies":
                    Poll(request);
                    return;

                case "POST" when path == "/lobbies":
                    CreateLobby(request, session);
                    return;

                case "GET" when path == "/match/state":
                    Poll(request);
                    return;

                case "POST" when path == "/match/intent":
                    Intent(request, session);
                    return;
            }

            var lobbyAction = LobbyAction(path);

            if (request.Method == "POST" && lobbyAction.action != null)
            {
                LobbyCommand(request, session, lobbyAction.lobbyId, lobbyAction.action);
                return;
            }

            Fail(request, 404, "BAD_REQUEST", "Unknown path");
        }

        private static (string lobbyId, string action) LobbyAction(string path)
        {
            if (!path.StartsWith("/lobbies/")) return (null, null);

            var parts = path.Substring("/lobbies/".Length).Split('/');
            if (parts.Length != 2) return (null, null);

            return parts[1] is "join" or "ready" or "leave" ? (parts[0], parts[1]) : (null, null);
        }

        private void Content(GatewayRequest request, string file)
        {
            if (!_content.IsReady)
            {
                Fail(request, 503, "INTERNAL", "Content is not loaded yet");
                return;
            }

            if (file != ContentService.CardsFileName)
            {
                Fail(request, 404, "BAD_REQUEST", "No such file");
                return;
            }

            Complete(request, 200, _content.CardsJson());
        }

        private void CreateSession(GatewayRequest request)
        {
            var payload = Parse<SessionRequest>(request);

            if (payload == null || string.IsNullOrWhiteSpace(payload.Name))
            {
                Fail(request, 400, "BAD_REQUEST", "Field name is required");
                return;
            }

            var session = new GatewaySession
            {
                SessionId = "s_" + Guid.NewGuid().ToString("N"),
                PlayerId = "p_" + payload.Name.GetHashCode().ToString("x8"),
                Name = payload.Name,
                Uuid = payload.Uuid,
                ExpiresAt = Now() + SessionLifetimeHours * 3600,
            };

            _sessions[session.SessionId] = session;

            Complete(request, 200, Json(new SessionWire
            {
                SessionId = session.SessionId,
                PlayerId = session.PlayerId,
                ExpiresAt = session.ExpiresAt,
                ContentVersion = _content.ContentVersion,
            }));
        }

        private void SessionState(GatewayRequest request, GatewaySession session)
        {
            var lobbyId = _lobbies.GetLobbyId(session.Seat);
            var context = _lobbies.GetGameContext(session.Seat);

            var phase = session.Outcome != null ? "ended"
                : context != null ? "match"
                : lobbyId != null ? "lobby"
                : "idle";

            Complete(request, 200, Json(new SessionStateWire
            {
                Phase = phase,
                LobbyId = lobbyId,
                MatchId = context != null ? lobbyId : null,
                StateVersion = session.Channel.Version,
            }));
        }

        private void Poll(GatewayRequest request)
        {
            request.Since = Since(request);

            if (!TryFinishPoll(request))
                Hold(request);
        }

        private void CreateLobby(GatewayRequest request, GatewaySession session)
        {
            var payload = Parse<CreateLobbyRequest>(request);

            if (payload == null)
            {
                Fail(request, 400, "BAD_REQUEST", "Malformed request body");
                return;
            }

            var message = new CreateLobbyMessage
            {
                Name = payload.Name ?? string.Empty,
                Password = payload.Password ?? string.Empty,
                Deck = payload.Deck,
            };

            var error = _lobbies.TryCreateLobby(session.Channel, ActiveSeat(session), message,
                out var seat, out var violations);

            if (error != LobbyError.None)
            {
                FailLobby(request, error, violations);
                return;
            }

            session.Seat = seat;

            Complete(request, 200, Json(new CreatedLobbyWire { LobbyId = _lobbies.GetLobbyId(seat) }));
        }

        private void LobbyCommand(GatewayRequest request, GatewaySession session, string lobbyId, string action)
        {
            if (action == "join")
            {
                JoinLobby(request, session, lobbyId);
                return;
            }

            if (session.Seat == null || _lobbies.GetLobbyId(session.Seat) != lobbyId)
            {
                Fail(request, 404, "LOBBY_NOT_FOUND", "Player is not in this lobby");
                return;
            }

            if (action == "ready")
                _lobbies.SetReady(session.Seat);
            else
                LeaveLobby(session);

            Complete(request, 200, Json(new OkWire()));
        }

        private void JoinLobby(GatewayRequest request, GatewaySession session, string lobbyId)
        {
            var payload = Parse<JoinLobbyRequest>(request);

            if (payload == null)
            {
                Fail(request, 400, "BAD_REQUEST", "Malformed request body");
                return;
            }

            var message = new JoinLobbyMessage
            {
                LobbyId = lobbyId,
                Password = payload.Password ?? string.Empty,
                Deck = payload.Deck,
            };

            var error = _lobbies.TryJoinLobby(session.Channel, ActiveSeat(session), message,
                out var seat, out var violations);

            if (error != LobbyError.None)
            {
                FailLobby(request, error, violations);
                return;
            }

            session.Seat = seat;

            Complete(request, 200, Json(new OkWire()));
        }

        private Seat ActiveSeat(GatewaySession session)
        {
            if (session.Seat == null) return null;
            if (_lobbies.GetLobbyId(session.Seat) != null) return session.Seat;

            session.Seat = null;
            session.LastState = null;

            return null;
        }

        private void LeaveLobby(GatewaySession session)
        {
            _game.OnLeave(session.Seat);

            session.Seat = null;
            session.LastState = null;
            session.Outcome = null;
            session.RoundResult = RoundResult.None;
        }

        private void Intent(GatewayRequest request, GatewaySession session)
        {
            var payload = Parse<IntentRequest>(request);

            if (payload == null || string.IsNullOrEmpty(payload.Type))
            {
                Fail(request, 400, "BAD_REQUEST", "Fields intentId and type are required");
                return;
            }

            if (session.TryGetIntent(payload.IntentId, out var remembered))
            {
                Complete(request, 200, Json(remembered));
                return;
            }

            var error = Apply(session, payload);

            if (error != IntentError.None)
            {
                var (status, code) = IntentCode(error);
                Fail(request, status, code, IntentText(error));
                return;
            }

            var answer = new IntentAcceptedWire { Version = session.Channel.Version };

            session.RememberIntent(payload.IntentId, answer);

            Complete(request, 200, Json(answer));
        }

        private IntentError Apply(GatewaySession session, IntentRequest payload)
        {
            var seat = session.Seat;

            switch (payload.Type)
            {
                case "play":
                    return _intents.PlayCard(seat, new PlayCardMessage
                    {
                        CardInstanceId = payload.CardInstanceId,
                        Row = Row(payload.Row),
                        SlotIndex = payload.SlotIndex,
                        TargetIds = payload.TargetIds ?? new string[0],
                    });

                case "pass":
                    return _intents.Pass(seat);

                case "redraw":
                    return _intents.Redraw(seat, payload.CardInstanceId);

                case "redrawReady":
                    return _intents.RedrawReady(seat);

                default:
                    return IntentError.WrongPhase;
            }
        }

        private static RowType Row(string row)
            => Enum.TryParse<RowType>(row, true, out var parsed) ? parsed : RowType.None;

        private bool TryFinishPoll(GatewayRequest request)
        {
            if (request.Path == "/lobbies")
                return TryFinishLobbyPoll(request);

            return TryFinishMatchPoll(request);
        }

        private bool TryFinishLobbyPoll(GatewayRequest request)
        {
            if (_lobbies.LobbiesVersion <= request.Since)
                return Expired(request);

            var payload = new LobbyListWire { Version = _lobbies.LobbiesVersion };

            foreach (var lobby in _lobbies.Lobbies)
            {
                payload.Lobbies.Add(new LobbyWire
                {
                    LobbyId = lobby.LobbyId,
                    Name = lobby.Name,
                    IsPrivate = lobby.IsPrivate,
                    Players = lobby.Players,
                    MaxPlayers = lobby.MaxPlayers,
                });
            }

            Complete(request, 200, Json(payload));
            return true;
        }

        private bool TryFinishMatchPoll(GatewayRequest request)
        {
            var session = _sessions.TryGetValue(request.SessionId, out var found) ? found : null;

            if (session == null)
            {
                Fail(request, 401, "UNAUTHORIZED", "Unknown session");
                return true;
            }

            var inMatch = _lobbies.GetGameContext(session.Seat) != null;

            if (!inMatch && session.LastState == null)
            {
                Fail(request, 404, "MATCH_NOT_FOUND", "Player is not in a match");
                return true;
            }

            if (!session.Channel.HasNewsSince(request.Since))
                return Expired(request);

            Complete(request, 200, Json(BuildMatchState(session)));
            return true;
        }

        private MatchStateWire BuildMatchState(GatewaySession session)
        {
            var events = DrainChannel(session, out var version);
            var context = _lobbies.GetGameContext(session.Seat);
            var player = context?.GetPlayer(session.Seat);

            if (player != null)
                session.LastState = GatewayMapper.State(
                    MatchSnapshotBuilder.Build(context, player),
                    _lobbies.GetLobbyId(session.Seat));

            var state = session.LastState;
            if (state == null) return null;

            state.Version = version;
            state.RoundResult = GatewayMapper.Result(session.RoundResult);
            state.Events = events;

            if (session.Outcome != null)
            {
                state.Phase = "ended";
                state.Outcome = new OutcomeWire
                {
                    Result = GatewayMapper.Result(session.Outcome.Result),
                    Reason = GatewayMapper.Reason(session.Outcome.Reason),
                };
            }

            return state;
        }

        private static List<Dictionary<string, object>> DrainChannel(GatewaySession session, out int version)
        {
            var events = new List<Dictionary<string, object>>();

            foreach (var message in session.Channel.Take(out version))
            {
                switch (message)
                {
                    case DamageDealtMessage damage:
                        foreach (var hit in damage.Hits)
                            events.Add(new Dictionary<string, object>
                            {
                                ["type"] = "damage",
                                ["source"] = hit.SourceInstanceId,
                                ["target"] = hit.TargetInstanceId,
                                ["amount"] = hit.Amount,
                            });
                        break;

                    case UnitsStateChangedMessage units:
                        foreach (var unit in units.Units.Where(u => u.IsDead))
                            events.Add(new Dictionary<string, object>
                            {
                                ["type"] = "died",
                                ["instanceId"] = unit.InstanceId,
                            });
                        break;

                    case CardDrawnMessage drawn:
                        events.Add(new Dictionary<string, object>
                        {
                            ["type"] = "drawn",
                            ["instanceId"] = drawn.Card.InstanceId,
                        });
                        break;

                    case CardRedrawnMessage redrawn:
                        events.Add(new Dictionary<string, object>
                        {
                            ["type"] = "redrawn",
                            ["removed"] = redrawn.RemovedInstanceId,
                            ["instanceId"] = redrawn.NewCard.InstanceId,
                        });
                        break;

                    case RoundEndedMessage round:
                        session.RoundResult = round.Result;
                        break;

                    case GameEndedMessage ended:
                        session.Outcome = new MatchOutcome
                        {
                            Result = ended.IsTie ? RoundResult.Tie
                                : ended.AmIWinner ? RoundResult.Win
                                : RoundResult.Lose,
                            Reason = MatchEndReason.Normal,
                        };
                        break;

                    case GiveUpMessage giveUp:
                        session.Outcome = new MatchOutcome
                        {
                            Result = giveUp.IsMyLose ? RoundResult.Lose : RoundResult.Win,
                            Reason = MatchEndReason.Surrender,
                        };
                        break;

                    case EnemyDisconnectedMessage:
                        session.Outcome = new MatchOutcome
                        {
                            Result = RoundResult.Win,
                            Reason = MatchEndReason.Disconnect,
                        };
                        break;
                }
            }

            return events;
        }

        private void Hold(GatewayRequest request)
        {
            request.Deadline = Time.realtimeSinceStartup + ContentService.PollTimeoutMs / 1000f;
            _waiting.Add(request);
        }

        private bool Expired(GatewayRequest request)
        {
            if (request.Deadline <= 0f) return false;
            if (Time.realtimeSinceStartup < request.Deadline) return false;

            Complete(request, 204, null);
            return true;
        }

        private static int Since(GatewayRequest request)
        {
            var raw = request.Query?["since"];

            return int.TryParse(raw, out var since) ? since : 0;
        }

        private void FailLobby(GatewayRequest request, LobbyError error, DeckViolation[] violations)
        {
            if (error == LobbyError.DeckInvalid)
            {
                Fail(request, 409, "DECK_INVALID", "Deck fails validation", new
                {
                    violations = violations.Select(Violation).ToArray()
                });
                return;
            }

            var (status, code, text) = LobbyCode(error);

            Fail(request, status, code, text);
        }

        private static ViolationWire Violation(DeckViolation violation)
        {
            var data = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(violation.CardId)) data["cardId"] = violation.CardId;
            if (!string.IsNullOrEmpty(violation.Field)) data["field"] = violation.Field;

            var expected = violation.Code switch
            {
                DeckViolationCode.TooManyCopies => "max",
                DeckViolationCode.DeckTooSmall => "min",
                DeckViolationCode.ContentMismatch => "expected",
                _ => null
            };

            if (expected != null) data[expected] = violation.Expected;

            data["actual"] = violation.Actual;

            return new ViolationWire
            {
                Code = DeckViolationCodes.ToWire(violation.Code),
                Data = data,
            };
        }

        private static (int status, string code, string text) LobbyCode(LobbyError error)
        {
            return error switch
            {
                LobbyError.LobbyNotFound => (404, "LOBBY_NOT_FOUND", "Lobby not found"),
                LobbyError.LobbyFull => (409, "LOBBY_FULL", "Lobby is full"),
                LobbyError.AlreadyInLobby => (409, "ALREADY_IN_LOBBY", "Player is already in a lobby"),
                LobbyError.InvalidPassword => (409, "INVALID_PASSWORD", "Invalid password"),
                _ => (500, "INTERNAL", "Request failed")
            };
        }

        private static (int status, string code) IntentCode(IntentError error)
        {
            return error switch
            {
                IntentError.MatchNotFound => (404, "MATCH_NOT_FOUND"),
                IntentError.MatchPaused => (409, "WRONG_PHASE"),
                IntentError.NotYourTurn => (409, "NOT_YOUR_TURN"),
                IntentError.WrongPhase => (409, "WRONG_PHASE"),
                IntentError.CardNotInHand => (409, "CARD_NOT_IN_HAND"),
                IntentError.IllegalRow => (409, "ILLEGAL_ROW"),
                IntentError.IllegalSlot => (409, "ILLEGAL_SLOT"),
                IntentError.IllegalTargets => (409, "ILLEGAL_TARGETS"),
                _ => (500, "INTERNAL")
            };
        }

        private static string IntentText(IntentError error)
        {
            return error switch
            {
                IntentError.MatchNotFound => "Player is not in a match",
                IntentError.MatchPaused => "Match is paused: the opponent is reconnecting",
                IntentError.NotYourTurn => "It is the opponent's turn",
                IntentError.WrongPhase => "This action is not available right now",
                IntentError.CardNotInHand => "No such card in hand",
                IntentError.IllegalRow => "A unit needs a Melee or Ranged row",
                IntentError.IllegalSlot => "The slot is taken or does not exist",
                IntentError.IllegalTargets => "Invalid targets",
                _ => "Action failed"
            };
        }

        private void Fail(GatewayRequest request, int status, string code, string message, object details = null)
        {
            Complete(request, status, Json(new ErrorWire
            {
                Code = code,
                Message = message,
                Details = details,
            }));
        }

        private static void Complete(GatewayRequest request, int status, byte[] body,
            string contentType = "application/json")
        {
            request.Completion.TrySetResult(new GatewayResult
            {
                Status = status,
                Body = body,
                ContentType = contentType,
            });
        }

        private static T Parse<T>(GatewayRequest request) where T : class
        {
            if (string.IsNullOrWhiteSpace(request.Body)) return null;

            try
            {
                return JsonConvert.DeserializeObject<T>(request.Body);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static byte[] Json(object payload)
            => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload, WireSettings));

        private static long Now() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        private class GatewayResult
        {
            public int Status;
            public byte[] Body;
            public string ContentType;
        }

        private class GatewayRequest
        {
            public string Method;
            public string Path;
            public NameValueCollection Query;
            public string Body;
            public string SessionId;
            public int Since;
            public float Deadline;

            public readonly TaskCompletionSource<GatewayResult> Completion = new();
        }
    }
}
