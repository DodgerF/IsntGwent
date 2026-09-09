using System;
using Mirror;

namespace IsntGwent.Scripts.Lobby.Core
{
    public class BotSeatChannel : ISeatChannel
    {
        private readonly Action<NetworkMessage> _sink;

        public BotSeatChannel(Action<NetworkMessage> sink)
        {
            _sink = sink;
        }

        public bool IsOnline => true;

        public void Send<T>(T message) where T : struct, NetworkMessage
        {
            _sink?.Invoke(message);
        }
    }
}
