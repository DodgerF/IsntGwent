using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct HpChangedMessage : NetworkMessage
    {
        public int MyHp;
        public int EnemyHp;
    }
}