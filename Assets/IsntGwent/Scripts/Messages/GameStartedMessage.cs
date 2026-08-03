using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct GameStartedMessage : NetworkMessage
    {
        public CardData[] CardsInHand;
        public int EnemyCardAmount;
    }
}