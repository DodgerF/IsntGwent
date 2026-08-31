using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Lobby.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Audio
{
    public class LobbyAudioBinder : IInitializable, IDisposable
    {
        private const float MenuDrawStep = 0.03f;
        private const int MenuDrawMaxSounds = 8;

        private readonly LobbyClientHandler _handler;
        private readonly InputRouter _input;
        private readonly DeckSelectService _deckSelect;
        private readonly AudioService _audio;
        private readonly CompositeDisposable _disposables = new();

        public LobbyAudioBinder(
            LobbyClientHandler handler,
            InputRouter input,
            DeckSelectService deckSelect,
            AudioService audio)
        {
            _handler = handler;
            _input = input;
            _deckSelect = deckSelect;
            _audio = audio;
        }

        public void Initialize()
        {
            _handler.OnPrivateRoomCreated
                .Subscribe(_ => _audio.Play("sting_lobby_created"))
                .AddTo(_disposables);

            _handler.OnError
                .Subscribe(_ => _audio.Play("ui_denied"))
                .AddTo(_disposables);

            _input.CardPressed
                .Subscribe(_ => _audio.Play("ui_click"))
                .AddTo(_disposables);

            _input.CardHovered
                .Subscribe(_ => _audio.Play("ui_hover"))
                .AddTo(_disposables);

            _deckSelect.SelectedDeck
                .Select(CountCards)
                .Select(count => count <= 0
                    ? Observable.Empty<long>()
                    : Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(MenuDrawStep)).Take(count))
                .Switch()
                .Subscribe(_ => _audio.Play("card_draw_menu"))
                .AddTo(_disposables);
        }

        private static int CountCards(DeckDefinition deck)
        {
            if (deck?.Cards == null)
                return 0;

            var ids = new HashSet<string>();
            foreach (var entry in deck.Cards)
            {
                if (entry.Count > 0)
                    ids.Add(entry.CardId);
            }

            return Mathf.Min(ids.Count, MenuDrawMaxSounds);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
