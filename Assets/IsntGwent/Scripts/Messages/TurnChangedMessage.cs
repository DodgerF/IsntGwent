using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct TurnChangedMessage : NetworkMessage
    {
        public bool IsMyTurn;
    }
}