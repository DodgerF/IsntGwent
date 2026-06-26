using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct PowerUpdatedMessage : NetworkMessage
    {
        public int OwnMeleePower;
        public int OwnRangedPower;
        public int OwnTotalPower;
    
        public int EnemyMeleePower;
        public int EnemyRangedPower;
        public int EnemyTotalPower;
    }
}