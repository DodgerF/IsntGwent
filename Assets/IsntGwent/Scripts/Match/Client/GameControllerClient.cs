using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.UI;
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
        [Inject] private readonly CardInstanceRegistry _instances;
        [Inject] private readonly ConnectionService _connectionService;
        [Inject] private readonly AnimationCoordinator _coordinator;
        [Inject] private readonly CardPreviewService _previewService;

        private static readonly TimeSpan PendingTimeout = TimeSpan.FromSeconds(5);

        private readonly CompositeDisposable _disposables = new();
        private readonly SerialDisposable _pendingTimeout = new();
        private readonly List<PendingHit> _pendingHits = new();

        private int _playSequence;
        private int _hitBatch;

        private readonly struct PendingHit
        {
            public readonly DamageInstance Hit;
            public readonly int PlaySequence;
            public readonly int Batch;

            public PendingHit(DamageInstance hit, int playSequence, int batch)
            {
                Hit = hit;
                PlaySequence = playSequence;
                Batch = batch;
            }
        }

        public void Initialize()
        {
            _pendingTimeout.AddTo(_disposables);

            _handler.OnGameStarted
                .Subscribe(OnGameStarted)
                .AddTo(_disposables);

            _handler.OnTurnChanged
                .Subscribe(msg => _coordinator.Enqueue(() =>
                {
                    ClearPending();
                    _matchState.IsMyTurn.Value = msg.IsMyTurn;
                    _matchState.TurnChanged.OnNext(Unit.Default);
                }))
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
                .Subscribe(msg => _coordinator.Enqueue(() => OnPowerUpdated(msg)))
                .AddTo(_disposables);

            _handler.OnCardDrawn
                .Subscribe(msg => _coordinator.Enqueue(() => OnCardDrawn(msg)))
                .AddTo(_disposables);

            _handler.OnEnemyCardDrawn
                .Subscribe(msg => _coordinator.Enqueue(
                    () => _matchState.EnemyCardAmount.Value = msg.EnemyCardAmount))
                .AddTo(_disposables);

            _handler.OnBoardSync
                .Subscribe(msg => _coordinator.Enqueue(() => OnBoardSync(msg)))
                .AddTo(_disposables);

            _handler.OnRoundEnded
                .Subscribe(msg => _coordinator.Enqueue(() => OnRoundEnded(msg)))
                .AddTo(_disposables);

            _handler.OnGameEnded
                .Subscribe(msg => _coordinator.Enqueue(() => OnGameEnded(msg)))
                .AddTo(_disposables);

            _handler.OnHpChanged
                .Subscribe(msg => _coordinator.Enqueue(() => OnHpChanged(msg)))
                .AddTo(_disposables);

            _handler.OnUnitsStateChanged
                .Subscribe(msg => _coordinator.EnqueueRoutine(() => UnitsStateChangedBeat(msg)))
                .AddTo(_disposables);

            _handler.OnDamageDealt
                .Subscribe(msg =>
                {
                    var batch = ++_hitBatch;

                    foreach (var hit in msg.Hits)
                        _pendingHits.Add(new PendingHit(hit, _playSequence, batch));

                    _coordinator.EnqueueRoutine(() => FlushPendingHits(batch));
                })
                .AddTo(_disposables);

            _handler.OnEnemyDisconnected
                .Subscribe(_ =>
                {
                    ClearAnimations();
                    _matchState.IsEnemyLeft.Value = true;
                    _matchState.IsGameEnded.Value = true;
                })
                .AddTo(_disposables);
            _handler.OnGiveUp
                .Subscribe(msg =>
                {
                    ClearAnimations();

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
                    ClearAnimations();
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

        private void ClearAnimations()
        {
            _coordinator.Clear();
            _pendingHits.Clear();
        }

        private IEnumerator UnitsStateChangedBeat(UnitsStateChangedMessage msg)
        {
            var buried = new List<UnitInstance>();

            foreach (var data in msg.Units)
            {
                var unit = _instances.Get(data.InstanceId) as UnitInstance;
                if (unit == null) continue;

                unit.CurrentPower.Value = data.CurrentPower;

                if (!data.IsDead) continue;

                var wasOwn = _matchState.OwnMeleeRow.Contains(unit) ||
                             _matchState.OwnRangedRow.Contains(unit) ||
                             _matchState.Hand.Contains(unit);

                _matchState.OwnMeleeRow.Remove(unit);
                _matchState.OwnRangedRow.Remove(unit);
                _matchState.EnemyMeleeRow.Remove(unit);
                _matchState.EnemyRangedRow.Remove(unit);

                if (wasOwn)
                    _matchState.OwnGraveyard.Add(unit);
                else
                    _matchState.EnemyGraveyard.Add(unit);

                buried.Add(unit);
            }

            if (buried.Count == 0)
            {
                yield return new WaitForSeconds(CardAnimConfig.PowerPopDuration);
                yield break;
            }

            yield return new WaitForSeconds(CardAnimConfig.GraveyardFlightDuration);

            foreach (var unit in buried)
                unit.CurrentPower.Value = unit.UnitDefinition.Power;
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
                var instance = _instances.GetOrCreate(cardData.DefinitionId, cardData.InstanceId);
                if (instance == null) continue;

                if (instance is UnitInstance unit)
                    unit.CurrentPower.Value = cardData.CurrentPower;

                collection.Add(instance);
            }
        }

        private void OnCardDrawn(CardDrawnMessage msg)
        {
            var instance = _instances.GetOrCreate(msg.Card.DefinitionId, msg.Card.InstanceId);
            if (instance == null) return;

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
                var instance = _instances.GetOrCreate(cardData.DefinitionId, cardData.InstanceId);
                if (instance == null) continue;

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
            var card = _instances.Get(msg.CardInstanceId);
            if (card == null) return;

            var sequence = ++_playSequence;

            _coordinator.EnqueueRoutine(() => PlayCardBeat(card, sequence, isEnemy: false, () =>
            {
                if (card is UnitInstance)
                {
                    var row = msg.Row == RowType.Melee
                        ? _matchState.OwnMeleeRow
                        : _matchState.OwnRangedRow;
                    row.Add(card);
                }
                else if (card is SpellInstance)
                {
                    _matchState.OwnGraveyard.Add(card);
                }
            }));
        }

        private void OnEnemyCardPlayed(EnemyCardPlayedMessage msg)
        {
            var instance = _instances.GetOrCreate(msg.DefinitionId, msg.CardInstanceId);
            if (instance == null) return;

            if (instance is UnitInstance unit)
                unit.CurrentPower.Value = msg.CurrentPower;

            var sequence = ++_playSequence;

            _coordinator.EnqueueRoutine(() => PlayCardBeat(instance, sequence, isEnemy: true, () =>
            {
                if (instance is UnitInstance)
                {
                    var row = msg.Row == RowType.Melee
                        ? _matchState.EnemyMeleeRow
                        : _matchState.EnemyRangedRow;
                    row.Add(instance);
                }
                else if (instance is SpellInstance spell)
                {
                    _matchState.EnemyGraveyard.Add(spell);
                }

                _matchState.EnemyCardAmount.Value = msg.CardAmount;
            }));
        }

        private IEnumerator PlayCardBeat(CardInstance card, int sequence, bool isEnemy, Action place)
        {
            if (isEnemy)
            {
                _previewService.ShowCard.OnNext(card);
                yield return new WaitForSeconds(CardAnimConfig.EnemyPreviewDuration);
                _previewService.HideCard.OnNext(Unit.Default);
            }

            _matchState.CardStaged.OnNext(card);

            if (card is UnitInstance)
            {
                place();
                yield return new WaitForSeconds(CardAnimConfig.PlayFlightDuration);
                yield return PlayHits(card.Id, sequence);
            }
            else
            {
                yield return PlayHits(card.Id, sequence);
                place();
                yield return new WaitForSeconds(CardAnimConfig.PlayFlightDuration);
            }
        }

        private IEnumerator PlayHits(Guid sourceId, int sequence)
        {
            var hits = ConsumePendingHits(sourceId, sequence);
            if (hits.Length == 0) yield break;

            _matchState.DamageDealt.OnNext(hits);
            yield return new WaitForSeconds(CardAnimConfig.ProjectileDuration);
        }

        private IEnumerator FlushPendingHits(int batch)
        {
            var hits = _pendingHits
                .Where(h => h.Batch == batch)
                .Select(h => h.Hit)
                .ToArray();

            if (hits.Length == 0) yield break;

            _pendingHits.RemoveAll(h => h.Batch == batch);

            _matchState.DamageDealt.OnNext(hits);

            yield return new WaitForSeconds(CardAnimConfig.ProjectileDuration);
        }

        private DamageInstance[] ConsumePendingHits(Guid sourceId, int sequence)
        {
            var id = sourceId.ToString();

            bool Matches(PendingHit h) => h.PlaySequence == sequence && h.Hit.SourceInstanceId == id;

            var hits = _pendingHits.Where(Matches).Select(h => h.Hit).ToArray();
            if (hits.Length > 0)
                _pendingHits.RemoveAll(Matches);

            return hits;
        }


        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}