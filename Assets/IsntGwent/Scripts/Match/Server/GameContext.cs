using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Match.Client;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class Player
    {
        public int Hp = 2;
        
        public readonly NetworkConnectionToClient Connection;

        public bool IsPassed = false;
        
        public readonly List<CardInstance> Hand = new();
        public readonly int MaxCardInHand = 10;
        public readonly List<CardInstance> Deck;
        
        public readonly List<UnitInstance> MeleeRow = new();
        public readonly List<UnitInstance> RangedRow = new();
        public readonly List<CardInstance> Graveyard = new();
        
        public int MeleePower => MeleeRow.Sum(u => u.CurrentPower.Value);
        public int RangedPower => RangedRow.Sum(u => u.CurrentPower.Value);
        public int TotalPower => MeleePower + RangedPower;
        
        public List<UnitInstance> GetRow(RowType row)
        {
            return row switch
            {
                RowType.Melee => MeleeRow,
                RowType.Ranged => RangedRow,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public Player(NetworkConnectionToClient connection, List<CardInstance> deck)
        {
            Connection = connection;
            Deck = deck;
        }
    }
    
    public class DamageRecord
    {
        public readonly CardInstance Source;
        public readonly UnitInstance Target;
        public readonly int Amount;

        public DamageRecord(CardInstance source, UnitInstance target, int amount)
        {
            Source = source;
            Target = target;
            Amount = amount;
        }
    }

    public class GameContext : IDisposable
    {
        [Inject] private readonly CardDatabase _cardDatabase;

        private readonly CompositeDisposable _disposables = new();
        private readonly List<UnitInstance> _changedUnits = new();
        private readonly List<DamageRecord> _damageRecords = new();
        private readonly Dictionary<UnitInstance, IDisposable> _unitSubscriptions = new();

        public Player Player1;
        public Player Player2;

        public readonly ReactiveProperty<bool> GameEnded = new();

        public readonly GameEventBus Events = new();

        public void Publish(IGameEvent gameEvent) => Events.Publish(gameEvent);
        
        public void AddDisposable(IDisposable disposable) => _disposables.Add(disposable);

        public void OnUnitAddedToRow(UnitInstance unit)
        {
            OnUnitRemovedFromRow(unit);

            _unitSubscriptions[unit] = unit.CurrentPower
                .Skip(1)
                .Subscribe(_ =>
                {
                    if (!_changedUnits.Contains(unit))
                        _changedUnits.Add(unit);
                });
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

        public void RecordDamage(CardInstance source, UnitInstance target, int amount)
        {
            if (target == null || amount <= 0) return;

            _damageRecords.Add(new DamageRecord(source, target, amount));
        }

        public List<DamageRecord> FlushDamageRecords()
        {
            var result = new List<DamageRecord>(_damageRecords);
            _damageRecords.Clear();
            return result;
        }

        public Player GetPlayer(NetworkConnectionToClient connection)
        {
            if (Player1.Connection == connection) return Player1;
            if (Player2.Connection == connection) return Player2;
            return null;
        }
        
        public Player CurrentPlayer;
        
        public void SetPlayers(PlayerLobby player1, PlayerLobby player2)
        {
            Player1 = new Player(player1.Connection, CreateDeck(player1.Deck));
            Player2 = new Player(player2.Connection, CreateDeck(player2.Deck));
        }

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