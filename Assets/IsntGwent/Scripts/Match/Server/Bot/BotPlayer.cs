using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Server;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotPlayer : IDisposable
    {
        [Inject] private readonly SeatRegistry _seats;
        [Inject] private readonly LobbyManager _lobby;
        [Inject] private readonly MatchIntentService _intents;
        [Inject] private IBotBrain _brain;

        private const int MaxRetries = 3;
        private const float RetryDelay = 2f;

        private readonly Subject<BotPlayer> _finished = new();

        private BotProfile _profile = new();
        private IDisposable _timer;
        private bool _done;
        private int _retries;

        public Seat Seat { get; private set; }
        public IObservable<BotPlayer> Finished => _finished;

        public void Create(BotProfile profile, DeckDefinition deck, IBotBrain brain = null)
        {
            _profile = profile ?? new BotProfile();

            if (brain != null) _brain = brain;

            var account = new AccountData
            {
                Id = "bot:" + Guid.NewGuid(),
                Nickname = _profile.Nickname,
            };

            Seat = _seats.Create(new BotSeatChannel(Receive), deck, account);
            Seat.IsBot = true;
            Seat.IsReady = true;
        }

        private void Receive(NetworkMessage message)
        {
            if (_done) return;

            switch (message)
            {
                case TurnChangedMessage turn when turn.IsMyTurn:
                    Schedule(TurnDelay(), TakeTurn);
                    break;

                case AimRequestMessage aim:
                    var pool = aim.TargetIds;
                    Schedule(_profile.AimDelay, () => TakeAim(pool));
                    break;

                case RedrawStartedMessage:
                    Schedule(_profile.RedrawDelay, TakeRedraw);
                    break;

                case PendingPlayMessage pending when pending.IsMine && pending.Cards is { Length: > 0 }:
                    Schedule(TurnDelay(), TakeTurn);
                    break;

                case GameEndedMessage:
                case GiveUpMessage:
                case EnemyDisconnectedMessage:
                    Schedule(_profile.FinishDelay, Finish);
                    break;
            }
        }

        private float TurnDelay()
        {
            var delay = UnityEngine.Random.Range(_profile.TurnDelayMin, _profile.TurnDelayMax);

            if (UnityEngine.Random.value < _profile.ThinkChance)
                delay += UnityEngine.Random.Range(_profile.ThinkBonusMin, _profile.ThinkBonusMax);

            return delay;
        }

        private void Schedule(float seconds, Action action)
        {
            _timer?.Dispose();

            _timer = Observable
                .Timer(TimeSpan.FromSeconds(Math.Max(0.05f, seconds)))
                .Subscribe(_ => Run(action));
        }

        private void Run(Action action)
        {
            if (_done) return;

            try
            {
                action();
            }
            catch (Exception exception)
            {
                Log.Error(LogTag.Bot, "move failed: " + exception);
            }
        }

        private void TakeTurn()
        {
            if (!Resolve(out var context, out var me)) return;

            if (context.IsPaused)
            {
                Schedule(1f, TakeTurn);
                return;
            }

            if (context.IsRedrawPhase)
            {
                Schedule(_profile.RedrawDelay, TakeRedraw);
                return;
            }

            if (context.PendingAim != null) return;
            if (context.CurrentPlayer != me) return;

            var move = _brain.Decide(context, me) ?? BotMove.Pass();
            var error = Apply(move);

            if (error == IntentError.None)
            {
                _retries = 0;
                return;
            }

            Log.Warn(LogTag.Bot, $"{move.Kind} rejected: {error} (match {context.MatchId})");

            if (move.Kind != BotMoveKind.Pass)
            {
                var fallback = _intents.Pass(Seat);

                if (fallback == IntentError.None)
                {
                    _retries = 0;
                    return;
                }

                Log.Warn(LogTag.Bot, $"fallback pass rejected: {fallback} (match {context.MatchId})");
            }

            if (_retries++ >= MaxRetries) return;

            Schedule(RetryDelay, TakeTurn);
        }

        private IntentError Apply(BotMove move)
        {
            if (move.Kind == BotMoveKind.Pass)
                return _intents.Pass(Seat);

            return _intents.PlayCard(Seat, new PlayCardMessage
            {
                CardInstanceId = move.Card.Id.ToString(),
                Row = move.Row,
                SlotIndex = move.Slot,
                EnemyRow = move.EnemyRow,
                TargetIds = move.TargetIds.ToArray(),
            });
        }

        private void TakeAim(string[] pool)
        {
            if (!Resolve(out var context, out var me)) return;
            if (context.PendingAim == null) return;

            var chosen = _brain.ChooseAim(context, me, pool ?? Array.Empty<string>()) ?? new List<string>();

            var error = _intents.AimTargets(Seat, new AimTargetMessage { TargetIds = chosen.ToArray() });

            if (error != IntentError.None)
                Log.Warn(LogTag.Bot, $"aim rejected: {error} (match {context.MatchId})");
        }

        private void TakeRedraw()
        {
            if (!Resolve(out var context, out var me)) return;
            if (!context.IsRedrawPhase || me.IsRedrawReady) return;

            if (me.RedrawsLeft > 0)
            {
                var cardId = _brain.ChooseRedraw(context, me);

                if (!string.IsNullOrEmpty(cardId) && _intents.Redraw(Seat, cardId) == IntentError.None)
                {
                    Schedule(_profile.RedrawDelay, TakeRedraw);
                    return;
                }
            }

            var error = _intents.RedrawReady(Seat);

            if (error != IntentError.None)
                Log.Warn(LogTag.Bot, $"redraw ready rejected: {error} (match {context.MatchId})");
        }

        private bool Resolve(out GameContext context, out Player me)
        {
            context = _lobby.GetGameContext(Seat);
            me = context?.GetPlayer(Seat);

            if (context != null && me != null) return true;

            Finish();
            return false;
        }

        private void Finish()
        {
            if (_done) return;

            _done = true;

            _timer?.Dispose();
            _timer = null;

            _lobby.LeaveLobby(Seat);

            _finished.OnNext(this);
        }

        public void Dispose()
        {
            _done = true;

            _timer?.Dispose();
            _timer = null;

            if (Seat != null)
                _seats.Release(Seat);

            _finished.Dispose();
        }
    }
}
