using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct DamageDealtMessage : NetworkMessage
    {
        public DamageInstance[] Hits;
    }

    public struct DamageInstance
    {
        public string SourceInstanceId;
        public string TargetInstanceId;
        public int Amount;
    }
}
