using System;
using IsntGwent.Scripts.Core;
using Mirror;
using UniRx;
using IsntGwent.Scripts.Diagnostics;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Network
{
    public class ConnectionService : IInitializable, IDisposable
    {
        private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(3);

        [Inject] private readonly SceneService _sceneService;

        public readonly ReactiveProperty<bool> IsConnectionLost = new(false);
        public readonly ReactiveProperty<bool> IsConnected = new(false);
        public readonly Subject<Unit> AttemptFailed = new();

        private readonly CompositeDisposable _disposables = new();
        private readonly SerialDisposable _reconnectLoop = new();
        private bool _autoReconnect;

        public void Initialize()
        {
            _reconnectLoop.AddTo(_disposables);
            
            IsConnected.Value = NetworkClient.isConnected;

            MyNetManager.ClientConnected
                .Subscribe(_ => OnConnected())
                .AddTo(_disposables);

            MyNetManager.ClientDisconnected
                .Subscribe(_ => OnLost())
                .AddTo(_disposables);
        }
        
        public void SetAutoReconnect(bool enabled)
        {
            _autoReconnect = enabled;

            if (enabled && IsConnectionLost.Value)
                StartReconnectLoop();
            else if (!enabled)
                _reconnectLoop.Disposable = null;
        }
        
        public void RetryNow()
        {
            if (NetworkManager.singleton == null) return;

            if (NetworkClient.active && !NetworkClient.isConnected)
            {
                NetworkManager.singleton.StopClient();
                return;
            }

            TryReconnect();
        }
        
        public void ReturnToMenu()
        {
            SetAutoReconnect(true);
            _sceneService.LoadMenu();
        }

        private void OnConnected()
        {
            _reconnectLoop.Disposable = null;

            IsConnectionLost.Value = false;
            IsConnected.Value = true;
        }

        private void OnLost()
        {
            AttemptFailed.OnNext(Unit.Default);

            if (IsConnectionLost.Value) return;

            Log.Info(LogTag.Net, "Connection lost");

            IsConnectionLost.Value = true;
            IsConnected.Value = false;

            if (_autoReconnect)
                StartReconnectLoop();
        }

        private void StartReconnectLoop()
        {
            _reconnectLoop.Disposable = Observable
                .Interval(ReconnectInterval)
                .Subscribe(_ => TryReconnect());
        }

        private void TryReconnect()
        {
            if (NetworkServer.active) return;
            if (NetworkClient.active) return;
            if (NetworkManager.singleton == null) return;

            NetworkManager.singleton.StartClient();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
