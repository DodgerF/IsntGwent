using System;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Lobby.Client;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Audio
{
    public class LobbyAudioBinder : IInitializable, IDisposable
    {
        private readonly LobbyClientHandler _handler;
        private readonly InputRouter _input;
        private readonly AudioService _audio;
        private readonly CompositeDisposable _disposables = new();

        public LobbyAudioBinder(LobbyClientHandler handler, InputRouter input, AudioService audio)
        {
            _handler = handler;
            _input = input;
            _audio = audio;
        }

        public void Initialize()
        {
            _handler.OnLobbyCreated
                .Subscribe(_ => _audio.Play("sting_lobby_created"))
                .AddTo(_disposables);

            _input.CardHovered
                .Subscribe(_ => _audio.Play("ui_hover"))
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
