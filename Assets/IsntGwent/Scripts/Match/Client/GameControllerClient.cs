using System;
using System.Linq;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.Client
{
    public class GameControllerClient : IInitializable, IDisposable
    {
        [Inject] private readonly MatchClientHandler _handler;
        [Inject] private readonly CardSelectionService _selectionService;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardDatabase _cardDatabase;
        [Inject] private readonly ConnectionService _connectionService;
        
        private static readonly TimeSpan PendingTimeout = TimeSpan.FromSeconds(5);

        private readonly CompositeDisposable _disposables = new();
        private readonly SerialDisposable _pendingTimeout = new();

        public void Initialize()
        {
            _pendingTimeout.AddTo(_disposables);

            _handler.OnGameStarted
                .Subscribe(OnGameStarted)
                .AddTo(_disposables);

            _handler.OnTurnChanged
                .Subscribe(msg =>
                {
                    ClearPending();
                    _matchState.IsMyTurn.Value = msg.IsMyTurn;
                    _matchState.TurnChanged.OnNext(Unit.Default);
                })
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
                .Where(_ => _matchState.IsMyTurn.Value && !_matchState.IsActionPending.Value)
                .Subscribe(e =>
                {
                    _handler.SendPlayCard(
                        e.Card.Id.ToString(),
                        e.Row?.row ?? RowType.None,
                        e.TargetIds.ToArray()
                    );
                    BeginPending();
                })
                .AddTo(_disposables);

            _matchState.PassRequested
                .Where(_ => _matchState.IsMyTurn.Value && !_matchState.IsActionPending.Value)
                .Subscribe(_ =>
                {
                    _handler.SendPass();
                    BeginPending();
                })
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
            
            _handler.OnEnemyDisconnected
                .Subscribe(_ =>
                {
                    _matchState.IsEnemyLeft.Value = true;
                    _matchState.IsGameEnded.Value = true;
                })
                .AddTo(_disposables);
            _handler.OnGiveUp
                .Subscribe(msg =>
                {
                    if (msg.IsMyLose)
                    {
                        _matchState.AmIGiveUp.Value = true;
                    }
                    else
                    {
                        _matchState.IsEnemyGiveUp.Value = true;
                    }

                    _matchState.IsGameEnded.Value = true;
                })
                .AddTo(_disposables);

            _connectionService.IsConnectionLost
                .Where(v => v)
                .First()
                .Subscribe(_ =>
                {
                    ClearPending();
                    _matchState.IsConnectionLost.Value = true;
                    _matchState.IsWaitingImageActive.Value = false;
                    _matchState.IsGameEnded.Value = true;
                })
                .AddTo(_disposables);
        }
        private void BeginPending()
        {
            _matchState.IsActionPending.Value = true;
            _pendingTimeout.Disposable = Observable
                .Timer(PendingTimeout)
                .Subscribe(_ =>
                {
                    Debug.LogWarning("Подтверждение действия не пришло — разблокируем ввод");
                    _matchState.IsActionPending.Value = false;
                });
        }

        private void ClearPending()
        {
            _pendingTimeout.Disposable = null;
            _matchState.IsActionPending.Value = false;
        }

        private void OnUnitsStateChanged(UnitsStateChangedMessage msg)
        {
            foreach (var data in msg.Units)
            {
                var unit = FindUnit(data.InstanceId);
                if (unit == null) continue;

                if (data.IsDead)
                {
                    var wasOwn = _matchState.OwnMeleeRow.Contains(unit) ||
                                 _matchState.OwnRangedRow.Contains(unit) ||
                                 _matchState.Hand.Contains(unit);
                    
                    _matchState.OwnMeleeRow.Remove(unit);
                    _matchState.OwnRangedRow.Remove(unit);
                    _matchState.EnemyMeleeRow.Remove(unit);
                    _matchState.EnemyRangedRow.Remove(unit);

                    if (unit is UnitInstance u)
                        u.CurrentPower.Value = u.UnitDefinition.Power;

                    if (wasOwn)
                        _matchState.OwnGraveyard.Add(unit);
                    else
                        _matchState.EnemyGraveyard.Add(unit);
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
            ClearPending();
            _matchState.IsMyTurn.Value = msg.IsMyTurn;
            _matchState.LastRoundResult.Value = msg.Result;
            _matchState.TurnChanged.OnNext(Unit.Default);

            _matchState.OwnMeleeRow.Clear();
            _matchState.OwnRangedRow.Clear();
            _matchState.EnemyMeleeRow.Clear();
            _matchState.EnemyRangedRow.Clear();
        }

        private void OnGameEnded(GameEndedMessage msg)
        {
            ClearPending();
            _matchState.IsTie.Value = msg.IsTie;
            _matchState.AmIWinner.Value = msg.AmIWinner;
            
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
            _matchState.IsWaitingImageActive.Value = false;
            _matchState.IsMyTurn.Value = msg.IsMyTurn;
            _matchState.TurnChanged.OnNext(Unit.Default);

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