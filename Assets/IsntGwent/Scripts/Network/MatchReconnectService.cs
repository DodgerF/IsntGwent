using System;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Lobby.Server;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Network
{
    public class MatchReconnectService : IInitializable, IDisposable
    {
        private static readonly TimeSpan ReconnectTimeout =
            LobbyManager.MatchGracePeriod + TimeSpan.FromSeconds(30);
        private const string TokenKey = "seat.token";

        [Inject] private readonly ConnectionService _connection;
        [Inject] private readonly SceneService _scenes;

        public readonly ReactiveProperty<bool> IsReconnecting = new(false);

        private readonly CompositeDisposable _disposables = new();
        private readonly SerialDisposable _timeout = new();

        private string _token;
        private bool _isResuming;
        private bool _isConfirmed;

        public bool HasSeat => !string.IsNullOrEmpty(_token);

        public void Initialize()
        {
            _timeout.AddTo(_disposables);

            _token = PlayerPrefs.GetString(TokenKey, string.Empty);
            _isConfirmed = false;

            MyNetManager.ClientConnected
                .Subscribe(_ => OnConnected())
                .AddTo(_disposables);

            MyNetManager.ClientDisconnected
                .Subscribe(_ => OnLost())
                .AddTo(_disposables);
        }

        public void TryResume()
        {
            if (NetworkServer.active) return;
            if (!HasSeat) return;
            if (IsReconnecting.Value) return;

            _isResuming = true;

            if (NetworkClient.isConnected)
                RequestSeat();
        }

        public void BeginSeat(string token)
        {
            _token = token;
            _isResuming = false;
            _isConfirmed = true;

            PlayerPrefs.SetString(TokenKey, token ?? string.Empty);
            PlayerPrefs.Save();
        }

        public void EndSeat()
        {
            _token = null;
            _isResuming = false;
            _isConfirmed = false;

            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.Save();

            Stop();
        }

        private void OnLost()
        {
            if (!HasSeat) return;
            if (!_isConfirmed) return;
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
            if (!IsReconnecting.Value && !_isResuming) return;

            RequestSeat();
        }

        private void RequestSeat()
        {
            NetworkClient.ReplaceHandler<ReconnectResultMessage>(OnResult);
            NetworkClient.Send(new ReconnectRequestMessage { Token = _token });
        }

        private void OnResult(ReconnectResultMessage msg)
        {
            if (!IsReconnecting.Value && !_isResuming) return;

            if (!msg.IsSuccess || msg.Phase == ReconnectPhase.None)
            {
                Fail();
                return;
            }

            _isResuming = false;
            _isConfirmed = true;
            Stop();
            _scenes.LoadGame();
        }

        private void Fail()
        {
            Debug.Log("Reconnect to match failed");

            EndSeat();
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
