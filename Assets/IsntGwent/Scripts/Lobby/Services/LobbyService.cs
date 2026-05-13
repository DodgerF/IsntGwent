using IsntGwent.Scripts.Lobby.Core;
using UniRx;

namespace IsntGwent.Scripts.Lobby.Services
{
    public class LobbyService
    {
        public readonly ReactiveCollection<LobbyData>Lobbies = new();
       
    }
}