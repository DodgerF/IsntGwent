using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct DeckSyncMessage : NetworkMessage
    {
        public CardData[] OwnDeck;
        public int EnemyDeckCount;
    }
}
