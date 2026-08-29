using Mirror;

namespace IsntGwent.Scripts.Lobby.Core
{
    public interface ISeatChannel
    {
        bool IsOnline { get; }

        void Send<T>(T message) where T : struct, NetworkMessage;
    }
}
