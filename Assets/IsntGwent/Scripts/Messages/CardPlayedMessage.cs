using IsntGwent.Scripts.Cards.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct OwnCardPlayedMessage : NetworkMessage
    {
        public string CardInstanceId;
        public RowType Row;
    }
    
    public struct EnemyCardPlayedMessage : NetworkMessage
    {
        public string CardInstanceId;
        public string DefinitionId;
        public int CurrentPower;
        public RowType Row;
        public int CardAmount;
    }

}