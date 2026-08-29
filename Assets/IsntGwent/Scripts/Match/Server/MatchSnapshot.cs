using IsntGwent.Scripts.Messages;

namespace IsntGwent.Scripts.Match.Server
{
    public enum MatchPhase
    {
        Redraw,
        Play,
        Ended
    }

    public enum MatchEndReason
    {
        Normal,
        Surrender,
        Disconnect
    }

    public class MatchLimits
    {
        public int MaxHand;
        public int StartHp;
        public int SlotsPerRow;
    }

    public class RedrawSnapshot
    {
        public int Left;
        public bool IAmReady;
        public bool EnemyReady;
    }

    public class MatchOutcome
    {
        public RoundResult Result;
        public MatchEndReason Reason;
    }

    public class SideSnapshot
    {
        public int Hp;
        public bool Passed;

        public CardData[] Hand;
        public int HandCount;
        public CardData[] Deck;
        public int DeckCount;

        public CardData[] Graveyard;
        public int GraveyardCount;

        public CardData[] MustPlay;

        public CardData[] MeleeRow;
        public CardData[] RangedRow;
        public RowStatusData[] RowStatus;

        public int MeleePower;
        public int RangedPower;
        public int TotalPower;
    }

    public class MatchSnapshot
    {
        public MatchPhase Phase;
        public int Round;
        public bool IsMyTurn;

        public MatchLimits Limits;
        public RedrawSnapshot Redraw;

        public SideSnapshot You;
        public SideSnapshot Enemy;

        public RoundResult RoundResult;
        public MatchOutcome Outcome;
    }
}
