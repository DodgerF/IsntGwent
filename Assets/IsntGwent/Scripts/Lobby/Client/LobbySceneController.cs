using System;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Network;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Lobby.Client
{
    public class LobbySceneController : IInitializable, IDisposable
    {
        [Inject] private readonly LobbyClientHandler _handler;
        [Inject] private readonly SceneService _scenes;
        [Inject] private readonly ConnectionService _connection;
        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            _connection.SetAutoReconnect(true);

            _handler.OnJoinedLobby
                .Subscribe(_ => LoadGameScene())
                .AddTo(_disposables);
        }

        private void LoadGameScene()
        {
            _scenes.LoadGame();
        }

        public void Dispose()
        {
            _connection.SetAutoReconnect(false);
            _disposables.Dispose();
        }
    }
}