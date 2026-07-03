using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct GameEndedMessage : NetworkMessage
    {
        public bool IsTie;
        public bool AmIWinner;
    }
}