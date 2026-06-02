using System;
using IsntGwent.Scripts.Game.Services;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Lobby.Network
{
    public class LobbyClientHandler : IInitializable, IDisposable
    {
        private SessionService _service;

        public LobbyClientHandler(SessionService service)
        {
            _service = service;
        }
        public void Initialize()
        {
            if (NetworkClient.active)
            {
                NetworkClient.RegisterHandler<CreateLobbyResultMessage>(OnCreateLobbyResult);
                NetworkClient.RegisterHandler<JoinLobbyResultMessage>(OnJoinLobbyResult);
            }
        }

        private void OnJoinLobbyResult(JoinLobbyResultMessage msg)
        {
            if (!msg.IsSuccess)
            {
                //_service.OnError.OnNext(msg.ErrorMessage);
                return;
            }
            _service.CurrentLobbyId.Value = msg.LobbyId;
            _service.OnJoinedLobby.OnNext(Unit.Default);
        }

        private void OnCreateLobbyResult(CreateLobbyResultMessage msg)
        {
            if (!msg.IsSuccess)
            {
                //_service.OnError.OnNext(msg.ErrorMessage);
                return;
            }
           
            _service.CurrentLobbyId.Value = msg.LobbyId;
            _service.OnJoinedLobby.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            
        }
    }
}