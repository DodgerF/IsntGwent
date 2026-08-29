using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct PendingPlayMessage : NetworkMessage
    {
        public CardData[] Cards;
        public bool IsMine;
    }
}
