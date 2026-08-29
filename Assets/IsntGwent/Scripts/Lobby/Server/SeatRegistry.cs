using System.Collections.Generic;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Lobby.Core;
using Mirror;

namespace IsntGwent.Scripts.Lobby.Server
{
    public class SeatRegistry
    {
        private readonly Dictionary<NetworkConnectionToClient, Seat> _byConnection = new();
        private readonly Dictionary<string, Seat> _byToken = new();

        public Seat Create(ISeatChannel channel, DeckDefinition deck)
        {
            var seat = new Seat(deck, channel);

            if (channel is MirrorSeatChannel { Connection: not null } mirror)
                _byConnection[mirror.Connection] = seat;

            _byToken[seat.Token] = seat;

            return seat;
        }

        public Seat Resolve(NetworkConnectionToClient conn)
        {
            if (conn == null) return null;

            return _byConnection.TryGetValue(conn, out var seat) ? seat : null;
        }

        public Seat GetByToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;

            return _byToken.TryGetValue(token, out var seat) ? seat : null;
        }

        public void Attach(Seat seat, NetworkConnectionToClient conn)
        {
            if (seat == null || conn == null) return;

            if (seat.Channel is MirrorSeatChannel mirror)
            {
                if (mirror.Connection != null)
                    _byConnection.Remove(mirror.Connection);

                mirror.Connection = conn;
            }
            else
            {
                seat.Channel = new MirrorSeatChannel(conn);
            }

            _byConnection[conn] = seat;
            seat.IsConnected = true;
        }

        public void Detach(NetworkConnectionToClient conn)
        {
            var seat = Resolve(conn);
            if (seat == null) return;

            _byConnection.Remove(conn);
            seat.IsConnected = false;

            if (seat.Channel is MirrorSeatChannel mirror && mirror.Connection == conn)
                mirror.Connection = null;
        }

        public void Release(Seat seat)
        {
            if (seat == null) return;

            _byToken.Remove(seat.Token);

            if (seat.Channel is MirrorSeatChannel { Connection: not null } mirror)
                _byConnection.Remove(mirror.Connection);
        }

        public void Clear()
        {
            _byConnection.Clear();
            _byToken.Clear();
        }
    }
}
