using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct CardDrawnMessage : NetworkMessage
    {
        public CardData Card;
    }
    
    public struct EnemyCardDrawnMessage : NetworkMessage
    {
        public int EnemyCardAmount;
    }
}