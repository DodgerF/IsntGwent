using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct DamageDealtMessage : NetworkMessage
    {
        public DamageInstance[] Hits;
    }

    public enum DamageKind : byte
    {
        Card,
        Weather
    }

    public struct DamageInstance
    {
        public string SourceInstanceId;
        public string SourceCardId;
        public string TargetInstanceId;
        public int Amount;
        public DamageKind Kind;
    }
}
