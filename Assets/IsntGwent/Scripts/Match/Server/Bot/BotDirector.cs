using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Server;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotDirector : IInitializable, IDisposable
    {
        [Inject] private readonly DiContainer _container;
        [Inject] private readonly LobbyManager _lobby;
        [Inject] private readonly DeckDatabase _decks;
        [Inject] private readonly BotProfileProvider _profiles;

        private readonly CompositeDisposable _disposables = new();
        private readonly Dictionary<Seat, IDisposable> _waiting = new();
        private readonly Dictionary<BotPlayer, IDisposable> _bots = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            _lobby.SearchStarted
                .Subscribe(BeginWait)
                .AddTo(_disposables);
        }

        private void BeginWait(Seat seat)
        {
            if (seat == null) return;

            CancelWait(seat);

            var profile = _profiles.Default;
            var delay = UnityEngine.Random.Range(profile.SearchWaitMin, profile.SearchWaitMax);

            _waiting[seat] = Observable
                .Timer(TimeSpan.FromSeconds(delay))
                .Subscribe(_ => OnWaitExpired(seat));
        }

        private void CancelWait(Seat seat)
        {
            if (!_waiting.TryGetValue(seat, out var timer)) return;

            timer.Dispose();
            _waiting.Remove(seat);
        }

        private void OnWaitExpired(Seat seat)
        {
            _waiting.Remove(seat);

            if (!_lobby.IsSearching(seat)) return;

            Spawn(seat);
        }

        private void Spawn(Seat seat)
        {
            var profile = _profiles.Default;

            if (!_decks.Contains(profile.DeckId))
            {
                Log.Warn(LogTag.Bot, $"deck not found: {profile.DeckId}");
                return;
            }

            var bot = _container.Instantiate<BotPlayer>();
            bot.Create(profile, _decks.Get(profile.DeckId));

            if (!_lobby.PairWithBot(seat, bot.Seat))
            {
                bot.Dispose();
                return;
            }

            _bots[bot] = bot.Finished.Subscribe(Release);

            Log.Info(LogTag.Bot, $"bot joined the match against {seat.Account?.Nickname}");
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
            foreach (var timer in _waiting.Values)
                timer.Dispose();
            _waiting.Clear();

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
