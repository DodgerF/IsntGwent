using IsntGwent.Scripts.Cards.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct RowStatusData : NetworkMessage
    {
        public RowType Row;
        public string CardId;

        public bool IsEmpty => string.IsNullOrEmpty(CardId);
    }
}
