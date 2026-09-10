using IsntGwent.Scripts.Accounts.Core;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct LoginMessage : NetworkMessage
    {
        public string Nickname;
    }

    public struct LoginResultMessage : NetworkMessage
    {
        public bool IsSuccess;
        public AccountError Error;
        public string Nickname;
        public int Points;
        public int Rank;
    }

    public struct NicknameCheckMessage : NetworkMessage
    {
        public string Nickname;
    }

    public struct NicknameCheckResultMessage : NetworkMessage
    {
        public string Nickname;
        public AccountError Error;
    }

    public struct LeaderboardRequestMessage : NetworkMessage
    {
    }

    public struct LeaderboardEntry
    {
        public string Nickname;
        public int Points;
        public int Wins;
        public int Losses;
        public int Ties;
    }

    public struct LeaderboardResultMessage : NetworkMessage
    {
        public LeaderboardEntry[] Top;
        public LeaderboardEntry Me;
        public int MyRank;
    }
}
