using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct RoundEndedMessage : NetworkMessage
    {
        public RoundResult Result;
    }

    public enum RoundResult { None, Win, Lose, Tie }
}