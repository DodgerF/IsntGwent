using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct CardRemovedFromHandMessage : NetworkMessage
    {
        public string CardInstanceId;
    }
}