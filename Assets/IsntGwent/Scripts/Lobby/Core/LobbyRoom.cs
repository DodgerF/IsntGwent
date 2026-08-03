using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Match.Client;
using Mirror;

namespace IsntGwent.Scripts.Lobby.Core
{
    public class LobbyRoom
    {
        public readonly LobbyData Data;
        public const int MaxPlayers = 2;
        public readonly string Password;
        private readonly List<PlayerLobby> _players;

        public IReadOnlyList<PlayerLobby> Players => _players;
        public bool TryAddPlayer(PlayerLobby playerLobby)
        {
            if (_players.Count >= MaxPlayers)
                return false;

            if (_players.Contains(playerLobby))
                return false;

            _players.Add(playerLobby);
            return true;
        }
        public bool IsFull => _players.Count == MaxPlayers;
        public bool AllReady => _players.Count > 0 && _players.All(p => p.IsReady);

        public LobbyRoom(LobbyData data, string password)
        {
            Data = data;
            Password = password;
            _players = new List<PlayerLobby>();
        }

        public PlayerLobby GetPlayer(NetworkConnectionToClient conn)
        {
            return _players.FirstOrDefault(p => p.Connection == conn);
        }

        public PlayerLobby GetOpponent(NetworkConnectionToClient conn)
        {
            return _players.FirstOrDefault(p => p.Connection != conn);
        }

        public void SetReady(NetworkConnectionToClient conn)
        {
            var player = GetPlayer(conn);
            if (player == null) return;

            player.IsReady = true;
        }

        public void RemovePlayer(NetworkConnectionToClient conn)
        {
            var player = GetPlayer(conn);
            if (player == null) return;

            _players.Remove(player);
        }
    }
}
