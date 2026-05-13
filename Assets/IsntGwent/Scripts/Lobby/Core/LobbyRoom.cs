using System.Collections.Generic;
using Mirror;

namespace IsntGwent.Scripts.Lobby.Core
{
    public class LobbyRoom
    {
        public readonly LobbyData Data;
        public const int MaxPlayers = 2;
        public readonly string Password;
        private readonly HashSet<NetworkConnectionToClient> _players;

        public IReadOnlyCollection<NetworkConnectionToClient> Players => _players;
        public bool TryAddPlayer(NetworkConnectionToClient conn)
        {
            if (_players.Count >= MaxPlayers)
                return false;

            return _players.Add(conn);
        }
        public bool IsFull => Players.Count == MaxPlayers;
        public LobbyRoom(LobbyData data, string password)
        {
            Data = data;
            Password = password;
            _players = new HashSet<NetworkConnectionToClient>();
        }
        
    }
}