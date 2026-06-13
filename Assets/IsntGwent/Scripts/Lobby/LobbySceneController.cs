using System;
using IsntGwent.Scripts.Lobby.Network;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Lobby
{
    public class LobbySceneController : IInitializable, IDisposable
    {
        [Inject] private readonly LobbyClientHandler _handler;
        [Inject] private readonly SceneService _scenes;
        private readonly CompositeDisposable _disposables = new();
    
        public void Initialize()
        {
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
            _disposables.Dispose();
        }
    }
}