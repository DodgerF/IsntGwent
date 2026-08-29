using System.Collections.Generic;
using System.Linq;

namespace IsntGwent.Scripts.Lobby.Core
{
    public class LobbyRoom
    {
        public readonly LobbyData Data;
        public const int MaxPlayers = 2;
        public readonly string Password;
        private readonly List<Seat> _seats;

        public IReadOnlyList<Seat> Seats => _seats;

        public bool TryAddSeat(Seat seat)
        {
            if (_seats.Count >= MaxPlayers)
                return false;

            if (_seats.Contains(seat))
                return false;

            _seats.Add(seat);
            return true;
        }

        public bool IsFull => _seats.Count == MaxPlayers;
        public bool AllReady => _seats.Count > 0 && _seats.All(s => s.IsReady);

        public LobbyRoom(LobbyData data, string password)
        {
            Data = data;
            Password = password;
            _seats = new List<Seat>();
        }

        public bool Contains(Seat seat) => seat != null && _seats.Contains(seat);

        public Seat GetOpponent(Seat seat)
        {
            return _seats.FirstOrDefault(s => s != seat);
        }

        public void RemoveSeat(Seat seat)
        {
            if (seat == null) return;

            _seats.Remove(seat);
        }
    }
}
