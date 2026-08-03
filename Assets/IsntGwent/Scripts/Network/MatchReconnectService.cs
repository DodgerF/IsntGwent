using System;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Network
{
    public class MatchReconnectService : IInitializable, IDisposable
    {
        private static readonly TimeSpan ReconnectTimeout = TimeSpan.FromSeconds(60);

        [Inject] private readonly ConnectionService _connection;
        [Inject] private readonly SceneService _scenes;

        public readonly ReactiveProperty<bool> IsReconnecting = new(false);

        private readonly CompositeDisposable _disposables = new();
        private readonly SerialDisposable _timeout = new();

        private string _token;

        public bool IsMatchActive => !string.IsNullOrEmpty(_token);

        public void Initialize()
        {
            _timeout.AddTo(_disposables);

            MyNetManager.ClientConnected
                .Subscribe(_ => OnConnected())
                .AddTo(_disposables);

            MyNetManager.ClientDisconnected
                .Subscribe(_ => OnLost())
                .AddTo(_disposables);
        }

        public void BeginMatch(string token)
        {
            _token = token;
        }

        public void EndMatch()
        {
            _token = null;
            Stop();
        }

        private void OnLost()
        {
            if (!IsMatchActive) return;
            if (IsReconnecting.Value) return;

            Debug.Log("Reconnecting to match");

            IsReconnecting.Value = true;
            _connection.SetAutoReconnect(true);

            _timeout.Disposable = Observable
                .Timer(ReconnectTimeout)
                .Subscribe(_ => Fail());
        }

        private void OnConnected()
        {
            if (!IsReconnecting.Value) return;

            NetworkClient.ReplaceHandler<ReconnectResultMessage>(OnResult);
            NetworkClient.Send(new ReconnectRequestMessage { Token = _token });
        }

        private void OnResult(ReconnectResultMessage msg)
        {
            if (!IsReconnecting.Value) return;

            if (!msg.IsSuccess)
            {
                Fail();
                return;
            }

            Stop();
            _scenes.LoadGame();
        }

        private void Fail()
        {
            Debug.Log("Reconnect to match failed");

            _token = null;
            Stop();
        }

        private void Stop()
        {
            _timeout.Disposable = null;
            IsReconnecting.Value = false;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
