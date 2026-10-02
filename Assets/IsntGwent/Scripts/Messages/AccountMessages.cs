using IsntGwent.Scripts.Accounts.Core;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    /// Личность игрока приезжает на этапе аутентификации соединения, а не отдельным логином:
    /// пустой токен = гость (редактор, десктоп), непустой = сессия, выданная сервером после itch.
    public struct AuthRequestMessage : NetworkMessage
    {
        public string SessionToken;
    }

    public struct AuthResponseMessage : NetworkMessage
    {
        public bool IsSuccess;
        public AccountError Error;
        public string Nickname;
        public int Points;
        public int Rank;
        public bool IsGuest;
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
