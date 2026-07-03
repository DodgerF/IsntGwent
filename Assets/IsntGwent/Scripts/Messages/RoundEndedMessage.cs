using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct RoundEndedMessage : NetworkMessage
    {
        public bool IsMyTurn;
        public RoundResult Result;
    }

    public enum RoundResult { None, Win, Lose, Tie }
}