using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct GameStartedMessage : NetworkMessage
    {
        public string[] CardsInHand;
        public bool IsMyTurn;
    }
}