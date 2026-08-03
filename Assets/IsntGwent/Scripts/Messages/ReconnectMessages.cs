using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct ReconnectRequestMessage : NetworkMessage
    {
        public string Token;
    }

    public struct ReconnectResultMessage : NetworkMessage
    {
        public bool IsSuccess;
    }

    public struct OpponentReconnectingMessage : NetworkMessage
    {
        public bool IsReconnecting;
    }

    public struct MatchSnapshotMessage : NetworkMessage
    {
        public CardData[] CardsInHand;
        public int EnemyCardAmount;

        public CardData[] OwnMeleeRow;
        public CardData[] OwnRangedRow;
        public CardData[] EnemyMeleeRow;
        public CardData[] EnemyRangedRow;
        public CardData[] OwnGraveyard;
        public CardData[] EnemyGraveyard;

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
    }
}
