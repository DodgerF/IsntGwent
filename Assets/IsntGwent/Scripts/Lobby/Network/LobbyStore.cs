using IsntGwent.Scripts.Lobby.Core;
using UniRx;

namespace IsntGwent.Scripts.Lobby
{
    public class LobbyStore
    {
        public readonly ReactiveCollection<LobbyData>Lobbies = new();
       
    }
}