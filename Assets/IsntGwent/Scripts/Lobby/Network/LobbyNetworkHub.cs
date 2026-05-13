using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Services;
using Mirror;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Lobby.Network
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class LobbyNetworkHub : NetworkBehaviour
    {
        public readonly SyncList<LobbyData> SyncLobbies = new();
        [Inject] private LobbyService _service;
        
        public override void OnStartClient()
        {
            SyncLobbies.Callback += OnSyncListChanged;
            
            if (_service != null && _service.Lobbies != null)
            {
                _service.Lobbies.Clear();
                foreach (var lobbyData in SyncLobbies)
                {
                    _service.Lobbies.Add(lobbyData);
                }
            }
        }

        public override void OnStopClient()
        {
            SyncLobbies.Callback -= OnSyncListChanged;
        }

        private void OnSyncListChanged(SyncList<LobbyData>.Operation op, int index, LobbyData oldData, LobbyData newData)
        {
            switch (op)
            {
                case SyncList<LobbyData>.Operation.OP_ADD:
                    _service.Lobbies.Add(newData);
                    break;
                case SyncList<LobbyData>.Operation.OP_REMOVEAT:
                    _service.Lobbies.RemoveAt(index);
                    break;
                default:
                    break;
            }
        }
    }
}