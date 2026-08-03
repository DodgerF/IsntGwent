using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct RedrawCardMessage : NetworkMessage
    {
        public string CardInstanceId;
    }

    public struct RedrawReadyMessage : NetworkMessage
    {
    }

    public struct RedrawStartedMessage : NetworkMessage
    {
        public int RedrawsLeft;
        public int RoundNumber;
    }

    public struct CardRedrawnMessage : NetworkMessage
    {
        public string RemovedInstanceId;
        public CardData NewCard;
        public int RedrawsLeft;
    }

    public struct RedrawEndedMessage : NetworkMessage
    {
    }
}
