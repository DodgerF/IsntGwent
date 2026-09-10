using Mirror;

namespace IsntGwent.Scripts.Lobby.Core
{
    public class MirrorSeatChannel : ISeatChannel
    {
        public NetworkConnectionToClient Connection;

        public MirrorSeatChannel(NetworkConnectionToClient connection)
        {
            Connection = connection;
        }

        public bool IsOnline => Connection != null;

        public void Send<T>(T message) where T : struct, NetworkMessage
        {
            if (!IsOnline) return;

            Connection.Send(message);
        }
    }
}
