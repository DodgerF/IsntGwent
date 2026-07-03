using IsntGwent.Scripts.Lobby.Core;
using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Lobby.Network
{
    public class LobbyStore
    {
        public readonly ReactiveCollection<LobbyData> Lobbies = new();
    }
}