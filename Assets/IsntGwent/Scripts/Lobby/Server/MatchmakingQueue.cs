using System.Collections.Generic;
using IsntGwent.Scripts.Lobby.Core;

namespace IsntGwent.Scripts.Lobby.Server
{
    public class MatchmakingQueue
    {
        private readonly List<Seat> _seats = new();

        public int Count => _seats.Count;

        public void Enqueue(Seat seat)
        {
            if (seat == null) return;
            if (_seats.Contains(seat)) return;

            _seats.Add(seat);
        }

        public bool Remove(Seat seat)
        {
            return seat != null && _seats.Remove(seat);
        }

        public bool Contains(Seat seat)
        {
            return seat != null && _seats.Contains(seat);
        }

        public bool TryDequeue(out Seat seat)
        {
            while (_seats.Count > 0)
            {
                seat = _seats[0];
                _seats.RemoveAt(0);

                if (seat is { IsConnected: true }) return true;
            }

            seat = null;
            return false;
        }

        public void Clear()
        {
            _seats.Clear();
        }
    }
}
