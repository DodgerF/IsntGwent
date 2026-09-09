using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Server;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Match.Server.Bot;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Tutorial.Server
{
    public class TutorialDirector : IInitializable, IDisposable
    {
        public const string DealAction = "deal";

        private const float TurnDelay = 1.4f;

        [Inject] private readonly DiContainer _container;
        [Inject] private readonly LobbyManager _lobby;
        [Inject] private readonly ServerHandler _serverHandler;
        [Inject] private readonly TutorialScriptProvider _script;
        [Inject] private readonly BotProfileProvider _profiles;
        [Inject] private readonly DeckService _deckService;

        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<BotPlayer, IDisposable> _bots = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            _lobby.TutorialRequested
                .Subscribe(OnRequested)
                .AddTo(_disposables);

            _serverHandler.OnTutorialAction
                .Subscribe(t => OnAction(t.seat, t.msg))
                .AddTo(_disposables);
        }

        private void OnRequested(Seat seat)
        {
            if (seat == null) return;

            var deck = _script.BotDeck();

            if (deck?.Cards == null || deck.Cards.Length == 0)
            {
                Log.Warn(LogTag.Tutorial, "bot deck is empty");
                return;
            }

            var bot = _container.Instantiate<BotPlayer>();
            bot.Create(SteadyProfile(), deck, new ScriptedBotBrain(_script.Script?.BotMoves));

            if (!_lobby.SeatTutorialBot(seat, bot.Seat))
            {
                bot.Dispose();
                return;
            }

            _bots[bot] = bot.Finished.Subscribe(Release);

            Log.Info(LogTag.Tutorial, $"tutorial started for {seat.Account?.Nickname}");
        }

        private BotProfile SteadyProfile()
        {
            var profile = (_profiles.Default ?? new BotProfile()).Copy();

            profile.TurnDelayMin = TurnDelay;
            profile.TurnDelayMax = TurnDelay;
            profile.ThinkChance = 0f;
            profile.ThinkBonusMin = 0f;
            profile.ThinkBonusMax = 0f;

            return profile;
        }

        private void OnAction(Seat seat, TutorialActionMessage msg)
        {
            if (msg.Action != DealAction) return;

            var context = _lobby.GetGameContext(seat);

            if (context is not { IsTutorial: true }) return;
            if (!context.IsScriptPaused) return;

            context.IsScriptPaused = false;

            var amount = _script.Script?.StartingHand ?? 0;
            if (amount <= 0) return;

            _deckService.DrawAndSync(context, context.Player1, amount);
            _deckService.DrawAndSync(context, context.Player2, amount);
        }

        private void Release(BotPlayer bot)
        {
            if (_bots.TryGetValue(bot, out var subscription))
            {
                subscription.Dispose();
                _bots.Remove(bot);
            }

            bot.Dispose();
        }

        public void Dispose()
        {
            foreach (var pair in _bots)
            {
                pair.Value.Dispose();
                pair.Key.Dispose();
            }

            _bots.Clear();

            _disposables.Dispose();
        }
    }
}
