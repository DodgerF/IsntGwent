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
        public readonly int SlotIndex;

        public CardPlayed(Player owner, CardInstance card, RowType row, int slotIndex)
        {
            Owner = owner;
            Card = card;
            Row = row;
            SlotIndex = slotIndex;
        }
    }

    public readonly struct UnitDied : IGameEvent
    {
        public readonly UnitInstance Unit;
        public readonly Player Owner;
        public readonly UnitInstance LeftNeighbor;
        public readonly UnitInstance RightNeighbor;
        public readonly RowType Row;
        public readonly int SlotIndex;
        public readonly CardInstance Killer;

        public UnitDied(UnitInstance unit, Player owner, UnitInstance leftNeighbor, UnitInstance rightNeighbor,
            RowType row, int slotIndex, CardInstance killer)
        {
            Unit = unit;
            Owner = owner;
            LeftNeighbor = leftNeighbor;
            RightNeighbor = rightNeighbor;
            Row = row;
            SlotIndex = slotIndex;
            Killer = killer;
        }
    }

    public readonly struct UnitDevoured : IGameEvent
    {
        public readonly UnitInstance Consumed;
        public readonly UnitInstance Consumer;
        public readonly Player Owner;

        public UnitDevoured(UnitInstance consumed, UnitInstance consumer, Player owner)
        {
            Consumed = consumed;
            Consumer = consumer;
            Owner = owner;
        }
    }

    public readonly struct UnitSummoned : IGameEvent
    {
        public readonly UnitInstance Unit;
        public readonly Player Owner;
        public readonly bool FromDeck;

        public UnitSummoned(UnitInstance unit, Player owner, bool fromDeck)
        {
            Unit = unit;
            Owner = owner;
            FromDeck = fromDeck;
        }
    }

    public readonly struct UnitMoved : IGameEvent
    {
        public readonly UnitInstance Unit;
        public readonly Player Owner;
        public readonly RowType FromRow;
        public readonly int FromSlot;
        public readonly RowType ToRow;
        public readonly int ToSlot;

        public UnitMoved(UnitInstance unit, Player owner, RowType fromRow, int fromSlot, RowType toRow, int toSlot)
        {
            Unit = unit;
            Owner = owner;
            FromRow = fromRow;
            FromSlot = fromSlot;
            ToRow = toRow;
            ToSlot = toSlot;
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
