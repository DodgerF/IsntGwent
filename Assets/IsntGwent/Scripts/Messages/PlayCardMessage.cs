using IsntGwent.Scripts.Cards.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct PlayCardMessage : NetworkMessage
    {
        public string CardInstanceId;
        public RowType Row;
        public bool EnemyRow;
        public int SlotIndex;
        public string[] TargetIds;
    }
}