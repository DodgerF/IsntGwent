using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Match.Server
{
    public interface IGameEvent
    {
    }

    public readonly struct TurnStarted : IGameEvent
    {
        public readonly Player Player;

        public TurnStarted(Player player) => Player = player;
    }

    public readonly struct TurnEnded : IGameEvent
    {
        public readonly Player Player;

        public TurnEnded(Player player) => Player = player;
    }

    public readonly struct CardPlayed : IGameEvent
    {
        public readonly Player Owner;
        public readonly CardInstance Card;
        public readonly RowType Row;

        public CardPlayed(Player owner, CardInstance card, RowType row)
        {
            Owner = owner;
            Card = card;
            Row = row;
        }
    }

    public readonly struct UnitDied : IGameEvent
    {
        public readonly UnitInstance Unit;
        public readonly Player Owner;

        public UnitDied(UnitInstance unit, Player owner)
        {
            Unit = unit;
            Owner = owner;
        }
    }

    public readonly struct RoundEnded : IGameEvent
    {
        public readonly Player Winner;
        public readonly bool IsTie;

        public RoundEnded(Player winner, bool isTie)
        {
            Winner = winner;
            IsTie = isTie;
        }
    }
}
