using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct BoardSyncMessage : NetworkMessage
    {
        public CardData[] OwnMeleeRow;
        public CardData[] OwnRangedRow;
        public CardData[] EnemyMeleeRow;
        public CardData[] EnemyRangedRow;
        public CardData[] OwnGraveyard;
        public CardData[] EnemyGraveyard;
    }
}