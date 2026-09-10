using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct AimRequestMessage : NetworkMessage
    {
        public string CardInstanceId;
        public string[] TargetIds;
    }

    public struct AimTargetMessage : NetworkMessage
    {
        public string[] TargetIds;
    }
}
