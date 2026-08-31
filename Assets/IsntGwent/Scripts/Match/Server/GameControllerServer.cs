using System;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Server;
using IsntGwent.Scripts.Messages;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class GameControllerServer : IInitializable, IDisposable
    {
        [Inject] private readonly MatchServerHandler _handler;
        [Inject] private readonly MatchServerNotifier _notifier;
        [Inject] private readonly LobbyManager _lobbyManager;
        [Inject] private readonly MatchIntentService _intents;
        [Inject] private readonly RedrawService _redrawService;
        [Inject] private readonly TriggeredEffectDispatcher _dispatcher;
        [Inject] private readonly WeatherService _weatherService;

        private readonly CompositeDisposable _disposables = new();

        private void Report(Seat seat, string intent, IntentError error, string details = null)
        {
            if (error == IntentError.None) return;

            var context = _lobbyManager.GetGameContext(seat);
            var player = context?.GetPlayer(seat);
            var nickname = player?.Seat?.Account?.Nickname ?? "?";
            var tail = string.IsNullOrEmpty(details) ? string.Empty : " — " + details;

            Log.Warn(LogTag.Match,
                $"{intent} rejected: {error} (match {context?.MatchId ?? "?"}, {nickname}){tail}");

            context?.Journal?.Rejected(player, intent, error.ToString(), details);
        }

        private static string Describe(PlayCardMessage msg)
        {
            var targets = msg.TargetIds == null || msg.TargetIds.Length == 0
                ? "нет"
                : string.Join(", ", msg.TargetIds);

            return $"card {msg.CardInstanceId} → {msg.Row}[{msg.SlotIndex}]" +
                   $"{(msg.EnemyRow ? " (чужой ряд)" : string.Empty)}, цели: {targets}";
        }

        private static string Describe(AimTargetMessage msg)
        {
            return "цели: " + (msg.TargetIds == null || msg.TargetIds.Length == 0
                ? "нет"
                : string.Join(", ", msg.TargetIds));
        }

        public void Initialize()
        {
            _handler.OnPlayCard
                .Subscribe(t => Report(t.seat, "PlayCard", _intents.PlayCard(t.seat, t.msg), Describe(t.msg)))
                .AddTo(_disposables);

            _handler.OnAimTargets
                .Subscribe(t => Report(t.seat, "AimTargets", _intents.AimTargets(t.seat, t.msg), Describe(t.msg)))
                .AddTo(_disposables);

            _handler.OnPass
                .Subscribe(seat => Report(seat, "Pass", _intents.Pass(seat)))
                .AddTo(_disposables);

            _handler.OnLeave
                .Subscribe(OnLeave)
                .AddTo(_disposables);

            _handler.OnRedrawCard
                .Subscribe(t => Report(t.seat, "Redraw", _intents.Redraw(t.seat, t.msg.CardInstanceId),
                    "card " + t.msg.CardInstanceId))
                .AddTo(_disposables);

            _handler.OnRedrawReady
                .Subscribe(seat => Report(seat, "RedrawReady", _intents.RedrawReady(seat)))
                .AddTo(_disposables);

            _redrawService.PhaseEnded
                .Subscribe(OnRedrawPhaseEnded)
                .AddTo(_disposables);
        }

        public void OnLeave(Seat seat)
        {
            if (seat == null) return;

            var context = _lobbyManager.GetGameContext(seat);
            var player = context?.GetPlayer(seat);

            if (player != null)
            {
                Log.Info(LogTag.Match,
                    $"leave: {player.Seat?.Account?.Nickname} (match {context.MatchId})");

                EndGameBySurrender(context, context.GetOpponent(player), notifyLoser: false);
            }

            _lobbyManager.LeaveLobby(seat);
        }

        private void OnRedrawPhaseEnded(GameContext context)
        {
            _notifier.NotifyTurnChanged(context.Player1, context.CurrentPlayer == context.Player1);
            _notifier.NotifyTurnChanged(context.Player2, context.CurrentPlayer == context.Player2);

            if (context.RoundNumber == 1)
                context.Publish(new TurnStarted(context.CurrentPlayer));
        }

        public void StartGame(GameContext context)
        {
            _dispatcher.Attach(context);
            _weatherService.Attach(context);

            DeckService.Shuffle(context.Player1.Deck);
            DeckService.Shuffle(context.Player2.Deck);

            DeckService.DrawCards(context.Player1, 10);
            DeckService.DrawCards(context.Player2, 10);

            var rnd = UnityEngine.Random.Range(0, 2);
            context.CurrentPlayer = rnd == 0 ? context.Player1 : context.Player2;

            context.Journal?.Deal(context);

            _notifier.NotifyGameStarted(context);
            _notifier.NotifyDecks(context);

            _redrawService.BeginPhase(context);
        }

        public void EndGameBySurrender(GameContext context, Player winner, bool notifyLoser = true)
        {
            if (context.GameEnded.Value) return;

            _notifier.NotifyGiveUp(winner, notifyLoser ? context.GetOpponent(winner) : null);

            context.EndReason = "surrender";
            context.Winner = winner;
            context.GameEnded.Value = true;
        }

        public void EndGameByDisconnect(GameContext context, Player winner)
        {
            if (context.GameEnded.Value) return;

            Log.Info(LogTag.Match,
                $"ended by disconnect: {winner?.Seat?.Account?.Nickname} wins (match {context.MatchId})");

            _notifier.NotifyEnemyDisconnected(winner);

            context.EndReason = "disconnect";
            context.Winner = winner;
            context.GameEnded.Value = true;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
