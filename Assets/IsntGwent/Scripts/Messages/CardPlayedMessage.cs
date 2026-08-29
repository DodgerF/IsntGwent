using IsntGwent.Scripts.Cards.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct OwnCardPlayedMessage : NetworkMessage
    {
        public string CardInstanceId;
        public RowType Row;
        public int SlotIndex;
    }

    public struct EnemyCardPlayedMessage : NetworkMessage
    {
        public string CardInstanceId;
        public string DefinitionId;
        public int CurrentPower;
        public int Armor;
        public RowType Row;
        public int SlotIndex;
        public int CardAmount;
    }

}