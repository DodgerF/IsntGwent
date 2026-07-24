using IsntGwent.Scripts.Lobby.Core;
using UniRx;

namespace IsntGwent.Scripts.Lobby.Client
{
    public class LobbyStore
    {
        public readonly ReactiveCollection<LobbyData> Lobbies = new();
    }
}