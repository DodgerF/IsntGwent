using System.Collections.Generic;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Match;
using Mirror;

namespace IsntGwent.Scripts.Lobby.Core
{
    public class LobbyRoom
    {
        public readonly LobbyData Data;
        public const int MaxPlayers = 2;
        public readonly string Password;
        private readonly HashSet<PlayerLobby> _players;

        public IReadOnlyCollection<PlayerLobby> Players => _players;
        public bool TryAddPlayer(PlayerLobby playerLobby)
        {
            if (_players.Count >= MaxPlayers)
                return false;

            return _players.Add(playerLobby);
        }
        public bool IsFull => Players.Count == MaxPlayers;
        public LobbyRoom(LobbyData data, string password)
        {
            Data = data;
            Password = password;
            _players = new HashSet<PlayerLobby>();
        }
        
    }
}