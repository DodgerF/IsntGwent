using IsntGwent.Scripts.Lobby.Core;
using Mirror;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Lobby.Network
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class LobbyNetworkHub : NetworkBehaviour
    {
        public readonly SyncList<LobbyData> SyncLobbies = new();
        [Inject] private LobbyStore _store;
        
        public override void OnStartClient()
        {
            SyncLobbies.Callback += OnSyncListChanged;
            
            if (_store != null && _store.Lobbies != null)
            {
                _store.Lobbies.Clear();
                foreach (var lobbyData in SyncLobbies)
                {
                    _store.Lobbies.Add(lobbyData);
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
                    _store.Lobbies.Add(newData);
                    break;
                case SyncList<LobbyData>.Operation.OP_REMOVEAT:
                    _store.Lobbies.RemoveAt(index);
                    break;
                default:
                    break;
            }
        }
    }
}