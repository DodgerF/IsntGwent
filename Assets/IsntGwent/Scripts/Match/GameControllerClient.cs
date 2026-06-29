using System;
using System.Linq;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Services;
using IsntGwent.Scripts.Messages;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    public class GameControllerClient : IInitializable, IDisposable
    {
        [Inject] private readonly MatchClientHandler _handler;
        [Inject] private readonly CardSelectionService _selectionService;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardDatabase _cardDatabase;
        
        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            _handler.OnGameStarted
                .Subscribe(OnGameStarted)
                .AddTo(_disposables);

            _handler.OnTurnChanged
                .Subscribe(msg => _matchState.IsMyTurn.Value = msg.IsMyTurn)
                .AddTo(_disposables);
            
            _handler.OnCardRemovedFromHand
                .Subscribe(OnCardRemovedFromHand)
                .AddTo(_disposables);

            _handler.OnOwnCardPlayed
                .Subscribe(OnOwnCardPlayed)
                .AddTo(_disposables);

            _handler.OnEnemyCardPlayed
                .Subscribe(OnEnemyCardPlayed)
                .AddTo(_disposables);
            
            _selectionService.CardPlayRequested
                .Where(_ => _matchState.IsMyTurn.Value)
                .Subscribe(e => _handler.SendPlayCard(
                    e.Card.Id.ToString(),
                    e.Row?.row ?? RowType.None,
                    e.TargetIds.ToArray()
                ))
                .AddTo(_disposables);
            
            _handler.OnPowerUpdated
                .Subscribe(OnPowerUpdated)
                .AddTo(_disposables);
            
            _handler.OnCardDrawn
                .Subscribe(OnCardDrawn)
                .AddTo(_disposables);

            _handler.OnEnemyCardDrawn
                .Subscribe(msg => _matchState.EnemyCardAmount.Value = msg.EnemyCardAmount)
                .AddTo(_disposables);
            
            _handler.OnBoardSync
                .Subscribe(OnBoardSync)
                .AddTo(_disposables);
            
            _handler.OnRoundEnded
                .Subscribe(OnRoundEnded)
                .AddTo(_disposables);

            _handler.OnGameEnded
                .Subscribe(OnGameEnded)
                .AddTo(_disposables);
            
            _handler.OnHpChanged
                .Subscribe(OnHpChanged)
                .AddTo(_disposables);
            
            _handler.OnUnitsStateChanged
                .Subscribe(OnUnitsStateChanged)
                .AddTo(_disposables);
        }
        private void OnUnitsStateChanged(UnitsStateChangedMessage msg)
        {
            foreach (var data in msg.Units)
            {
                var unit = FindUnit(data.InstanceId);
                if (unit == null) continue;

                if (data.IsDead)
                {
                    if (_matchState.OwnMeleeRow.Contains(unit) ||
                        _matchState.OwnRangedRow.Contains(unit) ||
                        _matchState.Hand.Contains(unit))
                    {
                        _matchState.OwnGraveyard.Add(unit);
                        
                    }
                    else
                        _matchState.EnemyGraveyard.Add(unit);
                    
                    if (unit is UnitInstance u)
                        u.CurrentPower.Value = u.UnitDefinition.Power;

                    
                }
                else
                {
                    if (unit is UnitInstance u)
                        u.CurrentPower.Value = data.CurrentPower;
                }
            }
        }
        
        private CardInstance FindUnit(string instanceId)
        {
            var all = _matchState.OwnMeleeRow
                .Concat(_matchState.OwnRangedRow)
                .Concat(_matchState.EnemyMeleeRow)
                .Concat(_matchState.EnemyRangedRow);

            return all.FirstOrDefault(c => c.Id.ToString() == instanceId);
        }
        
        private void OnHpChanged(HpChangedMessage msg)
        {
            _matchState.MyHp.Value = msg.MyHp;
            _matchState.EnemyHp.Value = msg.EnemyHp;
        }
        
        private void OnRoundEnded(RoundEndedMessage msg)
        {
            _matchState.IsMyTurn.Value = msg.IsMyTurn;
            _matchState.LastRoundResult.Value = msg.Result;

            _matchState.OwnMeleeRow.Clear();
            _matchState.OwnRangedRow.Clear();
            _matchState.EnemyMeleeRow.Clear();
            _matchState.EnemyRangedRow.Clear();
        }

        private void OnGameEnded(GameEndedMessage msg)
        {
            _matchState.AmIWinner.Value = msg.AmIWinner;
            _matchState.IsTie.Value = msg.IsTie;
            _matchState.IsMyTurn.Value = false;
            _matchState.IsGameEnded.Value = true;
        }
        
        private void OnBoardSync(BoardSyncMessage msg)
        {
            SyncCollection(_matchState.OwnMeleeRow, msg.OwnMeleeRow);
            SyncCollection(_matchState.OwnRangedRow, msg.OwnRangedRow);
            SyncCollection(_matchState.EnemyMeleeRow, msg.EnemyMeleeRow);
            SyncCollection(_matchState.EnemyRangedRow, msg.EnemyRangedRow);
            SyncCollection(_matchState.OwnGraveyard, msg.OwnGraveyard);
            SyncCollection(_matchState.EnemyGraveyard, msg.EnemyGraveyard);
        }
        
        
        private void SyncCollection(ReactiveCollection<CardInstance> collection, CardData[] data)
        {
            collection.Clear();
            foreach (var cardData in data)
            {
                var definition = _cardDatabase.Get(cardData.DefinitionId);
                var instance = CardFactory.Create(definition);
                instance.SetId(Guid.Parse(cardData.InstanceId));
                
                collection.Add(instance);
            }
        }
        
        private void OnCardDrawn(CardDrawnMessage msg)
        {
            var definition = _cardDatabase.Get(msg.Card.DefinitionId);
            var instance = CardFactory.Create(definition);
            instance.SetId(Guid.Parse(msg.Card.InstanceId));

            _matchState.Hand.Add(instance);
        }
        
        private void OnPowerUpdated(PowerUpdatedMessage msg)
        {
            _matchState.OwnMeleePower.Value = msg.OwnMeleePower;
            _matchState.OwnRangedPower.Value = msg.OwnRangedPower;
            _matchState.OwnTotalPower.Value = msg.OwnTotalPower;
    
            _matchState.EnemyMeleePower.Value = msg.EnemyMeleePower;
            _matchState.EnemyRangedPower.Value = msg.EnemyRangedPower;
            _matchState.EnemyTotalPower.Value = msg.EnemyTotalPower;
        }
        
        private void OnGameStarted(GameStartedMessage msg)
        {
            _matchState.IsMyTurn.Value = msg.IsMyTurn;

            _matchState.Hand.Clear();
            foreach (var cardData in msg.CardsInHand)
            {
                var definition = _cardDatabase.Get(cardData.DefinitionId);
                var instance = CardFactory.Create(definition);
                instance.SetId(Guid.Parse(cardData.InstanceId));

                _matchState.Hand.Add(instance);
            }

            _matchState.EnemyCardAmount.Value = msg.EnemyCardAmount;
        }
        
        private void OnCardRemovedFromHand(CardRemovedFromHandMessage msg)
        {
            var card = _matchState.Hand.FirstOrDefault(c => c.Id.ToString() == msg.CardInstanceId);
            if (card != null)
                _matchState.Hand.Remove(card);
        }

        private void OnOwnCardPlayed(OwnCardPlayedMessage msg)
        {
            var card = _matchState.Hand.FirstOrDefault(c => c.Id.ToString() == msg.CardInstanceId);
            if (card == null) return;

            if (card is UnitInstance)
            {
                var row = msg.Row == RowType.Melee ? _matchState.OwnMeleeRow : _matchState.OwnRangedRow;
                row.Add(card);
            }
            else if (card is SpellInstance)
            {
                _matchState.OwnGraveyard.Add(card);
            }

            
        }

        private void OnEnemyCardPlayed(EnemyCardPlayedMessage msg)
        {
            var definition = _cardDatabase.Get(msg.DefinitionId);
            var instance = CardFactory.Create(definition);
            instance.SetId(Guid.Parse(msg.CardInstanceId));

            if (instance is UnitInstance unit)
            {
                unit.CurrentPower.Value = msg.CurrentPower;
                var row = msg.Row == RowType.Melee ? _matchState.EnemyMeleeRow : _matchState.EnemyRangedRow;
                row.Add(instance);
            }
            else if (instance is SpellInstance spell)
            {
                _matchState.EnemyGraveyard.Add(spell);
            }
            
            _matchState.EnemyCardAmount.Value = msg.CardAmount;
        }

        
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}