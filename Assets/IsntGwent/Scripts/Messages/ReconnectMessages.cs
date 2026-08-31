using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public enum ReconnectPhase
    {
        None,
        Lobby,
        Match
    }

    public struct ReconnectRequestMessage : NetworkMessage
    {
        public string Token;
    }

    public struct ReconnectResultMessage : NetworkMessage
    {
        public bool IsSuccess;
        public ReconnectPhase Phase;
    }

    public struct OpponentReconnectingMessage : NetworkMessage
    {
        public bool IsReconnecting;
    }

    public struct MatchSnapshotMessage : NetworkMessage
    {
        public CardData[] CardsInHand;
        public int EnemyCardAmount;

        public CardData[] OwnDeck;
        public int EnemyDeckCount;

        public CardData[] OwnMeleeRow;
        public CardData[] OwnRangedRow;
        public CardData[] EnemyMeleeRow;
        public CardData[] EnemyRangedRow;
        public CardData[] OwnGraveyard;
        public CardData[] EnemyGraveyard;
        public RowStatusData[] OwnRowStatus;
        public RowStatusData[] EnemyRowStatus;

        public int OwnMeleePower;
        public int OwnRangedPower;
        public int OwnTotalPower;
        public int EnemyMeleePower;
        public int EnemyRangedPower;
        public int EnemyTotalPower;

        public int MyHp;
        public int EnemyHp;

        public bool IsMyTurn;
        public bool IsEnemyPassed;

        public bool IsRedrawPhase;
        public int RedrawsLeft;
        public bool IsRedrawReady;

        public int RoundNumber;

        public CardData[] PendingPlays;
        public bool IsPendingMine;

        public string[] AimTargetIds;
    }
}
