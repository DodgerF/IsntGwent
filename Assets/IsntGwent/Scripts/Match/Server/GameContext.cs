using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Match.Server.Journal;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class Player
    {
        public const int StartHp = 2;

        public int Hp = StartHp;

        public readonly Seat Seat;

        public bool IsConnected => Seat is { IsConnected: true };

        public bool IsPassed = false;

        public int RedrawsLeft;
        public bool IsRedrawReady;
        public readonly List<CardInstance> RedrawPile = new();

        public readonly List<CardInstance> Hand = new();
        public readonly List<CardInstance> PendingPlays = new();
        public CardInstance NextPendingPlay => PendingPlays.Count > 0 ? PendingPlays[0] : null;
        public readonly int MaxCardInHand = 10;
        public readonly List<CardInstance> Deck;
        
        public readonly BoardRow MeleeRow;
        public readonly BoardRow RangedRow;
        public readonly List<CardInstance> Graveyard = new();
        public readonly Dictionary<RowType, RowWeather> Weather = new();

        public void AddWeather(RowType row, CardInstance source)
        {
            if (source == null) return;
            if (row != RowType.Melee && row != RowType.Ranged) return;

            if (!Weather.TryGetValue(row, out var weather))
            {
                weather = new RowWeather();
                Weather[row] = weather;
            }

            weather.Source = source;
            weather.CardId = source.Definition.Id;
        }

        public RowWeather GetWeather(RowType row)
            => Weather.TryGetValue(row, out var weather) ? weather : null;

        public int MeleePower => MeleeRow.Sum(u => u.CurrentPower.Value);
        public int RangedPower => RangedRow.Sum(u => u.CurrentPower.Value);
        public int TotalPower => MeleePower + RangedPower;

        public BoardRow GetRow(RowType row)
        {
            return row switch
            {
                RowType.Melee => MeleeRow,
                RowType.Ranged => RangedRow,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public BoardSlot FindSlot(UnitInstance unit)
            => MeleeRow.SlotOf(unit) ?? RangedRow.SlotOf(unit);

        public BoardSlot FirstFreeSlot()
        {
            var index = MeleeRow.FirstFreeIndex;
            if (index >= 0) return MeleeRow.Slots[index];

            index = RangedRow.FirstFreeIndex;
            return index >= 0 ? RangedRow.Slots[index] : null;
        }

        public Player(Seat seat, List<CardInstance> deck)
        {
            Seat = seat;
            Deck = deck;

            MeleeRow = new BoardRow(this, RowType.Melee);
            RangedRow = new BoardRow(this, RowType.Ranged);
        }
    }
    
    public class PendingAim
    {
        public Player Caster;
        public Player BoardOwner;
        public CardInstance Card;
        public RowType Row;
        public int Slot;
        public bool EnemyRow;
        public bool FromPending;
        public int EffectIndex;
        public int DestroyedPower;
        public int KilledCount;
        public readonly List<string> TargetIds = new();
        public readonly List<string> Pool = new();
    }

    public class DamageRecord
    {
        public readonly CardInstance Source;
        public readonly UnitInstance Target;
        public readonly int Amount;
        public readonly bool IsVisible;
        public readonly DamageKind Kind;

        public DamageRecord(CardInstance source, UnitInstance target, int amount, bool isVisible = true,
            DamageKind kind = DamageKind.Card)
        {
            Source = source;
            Target = target;
            Amount = amount;
            IsVisible = isVisible;
            Kind = kind;
        }
    }

    public class UnitLinkRecord
    {
        public readonly CardInstance Source;
        public readonly UnitInstance Target;
        public readonly UnitLinkKind Kind;
        public readonly RowType TargetRow;
        public readonly int TargetSlot;

        public UnitLinkRecord(CardInstance source, UnitInstance target, UnitLinkKind kind,
            RowType targetRow = RowType.None, int targetSlot = -1)
        {
            Source = source;
            Target = target;
            Kind = kind;
            TargetRow = targetRow;
            TargetSlot = targetSlot;
        }
    }

    public class GameContext : IDisposable
    {
        [Inject] private readonly CardDatabase _cardDatabase;

        private readonly CompositeDisposable _disposables = new();
        private readonly List<UnitInstance> _changedUnits = new();
        private readonly List<DamageRecord> _damageRecords = new();
        private readonly List<UnitLinkRecord> _linkRecords = new();
        private readonly Dictionary<UnitInstance, IDisposable> _unitSubscriptions = new();
        private readonly Dictionary<UnitInstance, BoardSlot> _slotTakeovers = new();

        public Player Player1;
        public Player Player2;

        public int RoundNumber = 1;
        public bool IsRedrawPhase;

        public bool IsRanked;
        public bool IsTutorial;
        public bool IsScriptPaused;
        public bool IsVsBot => Player1?.Seat is { IsBot: true } || Player2?.Seat is { IsBot: true };
        public Player Winner;
        public bool IsTie;

        public string MatchId;
        public string EndReason = "normal";
        public DateTime StartedAt = DateTime.Now;
        public MatchJournal Journal;

        public readonly ReactiveProperty<bool> GameEnded = new();

        public readonly GameEventBus Events = new();

        public void Publish(IGameEvent gameEvent) => Events.Publish(gameEvent);

        public void AddDisposable(IDisposable disposable) => _disposables.Add(disposable);

        private bool _boardDirty;
        public void MarkBoardDirty() => _boardDirty = true;

        public bool FlushBoardDirty()
        {
            var dirty = _boardDirty;
            _boardDirty = false;
            return dirty;
        }

        public CardInstance CreateCard(string definitionId)
            => CardFactory.Create(_cardDatabase.Get(definitionId));

        public void OnUnitAddedToRow(UnitInstance unit)
        {
            OnUnitRemovedFromRow(unit);

            var subscriptions = new CompositeDisposable();

            subscriptions.Add(unit.CurrentPower.Skip(1).Subscribe(_ => MarkChanged(unit)));
            subscriptions.Add(unit.Armor.Skip(1).Subscribe(_ => MarkChanged(unit)));
            subscriptions.Add(unit.BasePower.Skip(1).Subscribe(_ => MarkChanged(unit)));

            _unitSubscriptions[unit] = subscriptions;
        }

        private void MarkChanged(UnitInstance unit)
        {
            if (!_changedUnits.Contains(unit))
                _changedUnits.Add(unit);
        }
        
        public void OnUnitRemovedFromRow(UnitInstance unit)
        {
            if (!_unitSubscriptions.TryGetValue(unit, out var subscription)) return;

            subscription.Dispose();
            _unitSubscriptions.Remove(unit);
        }

        public List<UnitInstance> FlushChangedUnits()
        {
            var result = new List<UnitInstance>(_changedUnits);
            _changedUnits.Clear();
            return result;
        }

        public void RecordDamage(CardInstance source, UnitInstance target, int amount,
            DamageKind kind = DamageKind.Card)
        {
            if (target == null || amount <= 0) return;

            if (source != null && kind != DamageKind.Weather)
                target.LastAttacker = source;

            _damageRecords.Add(new DamageRecord(source, target, amount, true, kind));

            Journal?.Damage(this, source, target, amount, kind);
        }

        public void RecordKill(CardInstance source, UnitInstance target)
        {
            if (target == null) return;

            _damageRecords.Add(new DamageRecord(source, target, 0, false));

            Journal?.Kill(this, source, target);
        }

        public void RecordLink(CardInstance source, UnitInstance target, UnitLinkKind kind,
            RowType targetRow = RowType.None, int targetSlot = -1)
        {
            if (source == null || target == null) return;

            _linkRecords.Add(new UnitLinkRecord(source, target, kind, targetRow, targetSlot));

            Journal?.Link(this, source, target, kind);
        }

        public List<UnitLinkRecord> FlushLinkRecords()
        {
            var links = new List<UnitLinkRecord>(_linkRecords);
            _linkRecords.Clear();
            return links;
        }

        public List<DamageRecord> FlushDamageRecords()
        {
            var result = new List<DamageRecord>(_damageRecords);
            _damageRecords.Clear();
            return result;
        }

        public Player GetPlayer(Seat seat)
        {
            if (seat == null) return null;
            if (Player1.Seat == seat) return Player1;
            if (Player2.Seat == seat) return Player2;
            return null;
        }

        public Player GetPlayerByToken(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            if (Player1.Seat.Token == token) return Player1;
            if (Player2.Seat.Token == token) return Player2;
            return null;
        }

        public bool IsPaused => IsScriptPaused || !Player1.IsConnected || !Player2.IsConnected;

        public Player CurrentPlayer;

        public void SetPlayers(Seat seat1, Seat seat2)
        {
            Player1 = new Player(seat1, CreateDeck(seat1.Deck));
            Player2 = new Player(seat2, CreateDeck(seat2.Deck));

            LinkBoard();
        }

        private void LinkBoard()
        {
            foreach (var rowType in new[] { RowType.Melee, RowType.Ranged })
            {
                var first = Player1.GetRow(rowType);
                var second = Player2.GetRow(rowType);

                LinkNeighbors(first);
                LinkNeighbors(second);

                for (var i = 0; i < BoardConfig.SlotsPerRow; i++)
                {
                    first.Slots[i].Opposite = second.Slots[i];
                    second.Slots[i].Opposite = first.Slots[i];
                }
            }
        }

        private static void LinkNeighbors(BoardRow row)
        {
            for (var i = 0; i < row.Slots.Length; i++)
            {
                row.Slots[i].Left = i > 0 ? row.Slots[i - 1] : null;
                row.Slots[i].Right = i < row.Slots.Length - 1 ? row.Slots[i + 1] : null;
            }
        }

        public BoardSlot FindSlot(UnitInstance unit)
            => Player1.FindSlot(unit) ?? Player2.FindSlot(unit);

        private List<CardInstance> CreateDeck(DeckDefinition deckDefinition)
        {
            List<CardInstance> deck = new();

            foreach (var cardEnty in deckDefinition.Cards)
            {
                var cardDefinition = _cardDatabase.Get(cardEnty.CardId);
                
                for (int amount = 0; amount < cardEnty.Count; amount++)
                {
                    var card = CardFactory.Create(cardDefinition);
                    deck.Add(card);
                }
            }
            
            return deck;
        }
        public Player GetOpponent(Player player)
        {
            return Player1 == player ? Player2 : Player1;
        }

        public bool HasLeadership(Player player, CardInstance exclude = null)
        {
            if (player == null) return false;

            return MaxPower(player, exclude) >= MaxPower(GetOpponent(player), exclude);
        }

        private static int MaxPower(Player player, CardInstance exclude)
        {
            var max = 0;

            foreach (var unit in player.MeleeRow.Concat(player.RangedRow))
            {
                if (unit == exclude) continue;
                if (unit.CurrentPower.Value > max) max = unit.CurrentPower.Value;
            }

            return max;
        }

        public bool HasOnBoard(Player player, string definitionId)
        {
            if (player == null || string.IsNullOrEmpty(definitionId)) return false;

            foreach (var unit in player.MeleeRow.Concat(player.RangedRow))
            {
                if (unit.Definition.Id == definitionId) return true;
            }

            return false;
        }

        public Player PendingPlayNotice;

        public PendingAim PendingAim;

        public void RequestSlotTakeover(UnitInstance unit, BoardSlot slot)
        {
            if (unit == null || slot == null) return;

            _slotTakeovers[unit] = slot;
        }

        public Dictionary<UnitInstance, BoardSlot> FlushSlotTakeovers()
        {
            var result = new Dictionary<UnitInstance, BoardSlot>(_slotTakeovers);
            _slotTakeovers.Clear();
            return result;
        }

        public void Dispose()
        {
            foreach (var subscription in _unitSubscriptions.Values)
                subscription.Dispose();
            _unitSubscriptions.Clear();

            Events.Dispose();
            _disposables.Dispose();
        }
    }
}