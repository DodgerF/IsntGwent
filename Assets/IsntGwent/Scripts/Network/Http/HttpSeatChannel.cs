using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Lobby.Core;
using Mirror;

namespace IsntGwent.Scripts.Network.Http
{
    public class HttpSeatChannel : ISeatChannel
    {
        private static readonly object[] Nothing = new object[0];

        private readonly object _lock = new();
        private readonly List<object> _outgoing = new();

        public bool IsOnline => true;

        public int Version { get; private set; }

        public void Send<T>(T message) where T : struct, NetworkMessage
        {
            lock (_lock)
            {
                _outgoing.Add(message);
                Version++;
            }
        }

        public IReadOnlyList<object> Take(out int version)
        {
            lock (_lock)
            {
                version = Version;

                if (_outgoing.Count == 0) return Nothing;

                var taken = _outgoing.ToArray();
                _outgoing.Clear();

                return taken;
            }
        }

        public bool HasNewsSince(int since) => Version > Math.Max(since, 0);
    }
}
