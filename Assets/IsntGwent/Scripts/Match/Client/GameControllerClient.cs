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
        [Inject] private readonly MatchReconnectService _reconnectService;
        [Inject] private readonly AnimationCoordinator _coordinator;
        [Inject] private readonly CardPreviewService _previewService;

        private static readonly TimeSpan PendingTimeout = TimeSpan.FromSeconds(5);

        private readonly CompositeDisposable _disposables = new();
        private readonly SerialDisposable _pendingTimeout = new();
        private readonly List<PendingHit> _pendingHits = new();
        private readonly List<PendingState> _pendingStates = new();

        private int _playSequence;
        private int _hitBatch;
        private int _stateSequence;

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

        private readonly struct PendingState
        {
            public readonly int Id;
            public readonly int Batch;
            public readonly UnitsStateChangedMessage Message;

            public PendingState(int id, int batch, UnitsStateChangedMessage message)
            {
                Id = id;
                Batch = batch;
                Message = message;
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
                .Where(_ => !_matchState.IsMatchPaused.Value)
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
                .Where(_ => !_matchState.IsMatchPaused.Value)
                .Subscribe(_ =>
                {
                    _handler.SendPass();
                    _matchState.PassSent.OnNext(Unit.Default);
                    BeginPending();
                })
                .AddTo(_disposables);
            
            _handler.OnPowerUpdated
                .Subscribe(msg => _coordinator.Enqueue(() => OnPowerUpdated(msg)))
                .AddTo(_disposables);

            _handler.OnCardDrawn
                .Subscribe(msg => _coordinator.Enqueue(
                    () => OnCardDrawn(msg), CardAnimConfig.DrawBeatDuration))
                .AddTo(_disposables);

            _handler.OnEnemyCardDrawn
                .Subscribe(msg => _coordinator.Enqueue(
                    () =>
                    {
                        _matchState.EnemyCardAmount.Value = msg.EnemyCardAmount;
                        _matchState.EnemyCardDrawn.OnNext(Unit.Default);
                    },
                    CardAnimConfig.EnemyDrawFlightDuration))
                .AddTo(_disposables);

            _handler.OnRedrawStarted
                .Subscribe(msg => _coordinator.Enqueue(() => OnRedrawStarted(msg)))
                .AddTo(_disposables);

            _handler.OnCardRedrawn
                .Subscribe(msg => _coordinator.EnqueueRoutine(() => CardRedrawnBeat(msg)))
                .AddTo(_disposables);

            _handler.OnRedrawEnded
                .Subscribe(_ => _coordinator.Enqueue(
                    OnRedrawEnded, CardAnimConfig.PlayFlightDuration))
                .AddTo(_disposables);

            _matchState.RedrawRequested
                .Where(_ => _matchState.IsRedrawPhase.Value)
                .Where(_ => !_matchState.IsRedrawReady.Value)
                .Where(_ => !_matchState.IsActionPending.Value)
                .Where(_ => !_matchState.IsMatchPaused.Value)
                .Where(_ => _matchState.RedrawsLeft.Value > 0)
                .Subscribe(card =>
                {
                    _handler.SendRedrawCard(card.Id.ToString());
                    BeginPending();
                })
                .AddTo(_disposables);

            _matchState.RedrawReadyRequested
                .Where(_ => _matchState.IsRedrawPhase.Value)
                .Where(_ => !_matchState.IsRedrawReady.Value)
                .Subscribe(_ =>
                {
                    _handler.SendRedrawReady();
                    _matchState.IsRedrawReady.Value = true;
                })
                .AddTo(_disposables);

            _handler.OnBoardSync
                .Subscribe(msg => _coordinator.Enqueue(
                    () => OnBoardSync(msg), CardAnimConfig.RoundClearBeatDuration))
                .AddTo(_disposables);

            _handler.OnRoundEnded
                .Subscribe(msg => _coordinator.Enqueue(
                    () => OnRoundEnded(msg), CardAnimConfig.RoundResultBeatDuration))
                .AddTo(_disposables);

            _handler.OnGameEnded
                .Subscribe(msg => _coordinator.Enqueue(() => OnGameEnded(msg)))
                .AddTo(_disposables);

            _handler.OnHpChanged
                .Subscribe(msg => _coordinator.Enqueue(
                    () => OnHpChanged(msg), CardAnimConfig.HpBeatDuration))
                .AddTo(_disposables);

            _handler.OnUnitsStateChanged
                .Subscribe(msg =>
                {
                    var id = ++_stateSequence;
                    _pendingStates.Add(new PendingState(id, _hitBatch, msg));

                    _coordinator.EnqueueRoutine(() => UnitsStateChangedBeat(id));
                })
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

            _handler.OnSnapshot
                .Subscribe(OnSnapshot)
                .AddTo(_disposables);

            _handler.OnOpponentReconnecting
                .Subscribe(msg =>
                {
                    _matchState.IsOpponentReconnecting.Value = msg.IsReconnecting;
                    RefreshPause();
                })
                .AddTo(_disposables);

            _matchState.IsGameEnded
                .Where(v => v)
                .Take(1)
                .Subscribe(_ => _reconnectService.EndMatch())
                .AddTo(_disposables);

            _connectionService.IsConnectionLost
                .Where(v => v)
                .First()
                .Subscribe(_ => OnConnectionLost())
                .AddTo(_disposables);

            _reconnectService.IsReconnecting
                .Where(v => !v)
                .Skip(1)
                .Where(_ => !_reconnectService.IsMatchActive)
                .Subscribe(_ => EndByConnectionLost())
                .AddTo(_disposables);
        }

        private void OnConnectionLost()
        {
            ClearAnimations();
            ClearPending();

            if (_reconnectService.IsMatchActive && !_matchState.IsGameEnded.Value)
            {
                _matchState.IsSelfReconnecting.Value = true;
                RefreshPause();
                return;
            }

            EndByConnectionLost();
        }

        private void RefreshPause()
        {
            _matchState.IsMatchPaused.Value =
                _matchState.IsSelfReconnecting.Value || _matchState.IsOpponentReconnecting.Value;
        }

        private void EndByConnectionLost()
        {
            if (_matchState.IsGameEnded.Value) return;

            ClearAnimations();
            ClearPending();
            _matchState.IsConnectionLost.Value = true;
            _matchState.IsWaitingImageActive.Value = false;
            _matchState.IsGameEnded.Value = true;
        }

        private void OnSnapshot(MatchSnapshotMessage msg)
        {
            ClearAnimations();
            ClearPending();

            SyncCollection(_matchState.Hand, msg.CardsInHand);
            _matchState.EnemyCardAmount.Value = msg.EnemyCardAmount;

            SyncCollection(_matchState.OwnMeleeRow, msg.OwnMeleeRow);
            SyncCollection(_matchState.OwnRangedRow, msg.OwnRangedRow);
            SyncCollection(_matchState.EnemyMeleeRow, msg.EnemyMeleeRow);
            SyncCollection(_matchState.EnemyRangedRow, msg.EnemyRangedRow);
            SyncCollection(_matchState.OwnGraveyard, msg.OwnGraveyard);
            SyncCollection(_matchState.EnemyGraveyard, msg.EnemyGraveyard);

            _matchState.OwnMeleePower.Value = msg.OwnMeleePower;
            _matchState.OwnRangedPower.Value = msg.OwnRangedPower;
            _matchState.OwnTotalPower.Value = msg.OwnTotalPower;
            _matchState.EnemyMeleePower.Value = msg.EnemyMeleePower;
            _matchState.EnemyRangedPower.Value = msg.EnemyRangedPower;
            _matchState.EnemyTotalPower.Value = msg.EnemyTotalPower;

            _matchState.MyHp.Value = msg.MyHp;
            _matchState.EnemyHp.Value = msg.EnemyHp;

            _matchState.RedrawsLeft.Value = msg.RedrawsLeft;
            _matchState.IsRedrawReady.Value = msg.IsRedrawReady;
            _matchState.IsRedrawPhase.Value = msg.IsRedrawPhase;

            _matchState.IsOpponentReconnecting.Value = false;
            _matchState.IsSelfReconnecting.Value = false;
            RefreshPause();

            _matchState.IsWaitingImageActive.Value = false;

            _matchState.IsMyTurn.Value = msg.IsMyTurn;
            _matchState.TurnChanged.OnNext(Unit.Default);
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
            _pendingStates.Clear();
        }

        private IEnumerator UnitsStateChangedBeat(int id)
        {
            var index = _pendingStates.FindIndex(s => s.Id == id);
            if (index < 0) yield break;

            var message = _pendingStates[index].Message;
            _pendingStates.RemoveAt(index);

            yield return ApplyUnitStates(message);
        }

        private IEnumerator ApplyUnitStates(UnitsStateChangedMessage msg)
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

            _matchState.UnitsDied.OnNext(buried.Count);

            yield return new WaitForSeconds(CardAnimConfig.GraveyardFlightDuration);

            foreach (var unit in buried)
                unit.CurrentPower.Value = unit.UnitDefinition.Power;
        }


        private void OnHpChanged(HpChangedMessage msg)
        {
            var lost = 0;
            if (msg.MyHp < _matchState.MyHp.Value) lost++;
            if (msg.EnemyHp < _matchState.EnemyHp.Value) lost++;

            _matchState.MyHp.Value = msg.MyHp;
            _matchState.EnemyHp.Value = msg.EnemyHp;

            if (lost > 0)
                _matchState.HpLost.OnNext(lost);
        }

        private void OnRoundEnded(RoundEndedMessage msg)
        {
            ClearPending();
            _matchState.LastRoundResult.Value = msg.Result;
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

        private void OnRedrawStarted(RedrawStartedMessage msg)
        {
            ClearPending();
            _matchState.IsRedrawReady.Value = false;
            _matchState.RedrawsLeft.Value = msg.RedrawsLeft;
            _matchState.IsRedrawPhase.Value = true;
        }

        private void OnRedrawEnded()
        {
            _matchState.IsRedrawReady.Value = false;
            _matchState.RedrawsLeft.Value = 0;
            _matchState.IsRedrawPhase.Value = false;
        }

        private IEnumerator CardRedrawnBeat(CardRedrawnMessage msg)
        {
            ClearPending();

            var removed = _instances.Get(msg.RemovedInstanceId);

            if (removed != null)
            {
                _matchState.Hand.Remove(removed);
                _matchState.CardRedrawn.OnNext(removed);

                yield return new WaitForSeconds(CardAnimConfig.RedrawDiscardDuration);
            }

            _matchState.RedrawsLeft.Value = msg.RedrawsLeft;

            var drawn = _instances.GetOrCreate(msg.NewCard.DefinitionId, msg.NewCard.InstanceId);

            if (drawn != null)
            {
                _matchState.Hand.Add(drawn);
                _matchState.CardDrawn.OnNext(Unit.Default);

                yield return new WaitForSeconds(CardAnimConfig.DrawBeatDuration);
            }

            if (msg.RedrawsLeft <= 0)
                _matchState.RedrawReadyRequested.OnNext(Unit.Default);
        }

        private void OnCardDrawn(CardDrawnMessage msg)
        {
            var instance = _instances.GetOrCreate(msg.Card.DefinitionId, msg.Card.InstanceId);
            if (instance == null) return;

            _matchState.Hand.Add(instance);
            _matchState.CardDrawn.OnNext(Unit.Default);
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
            _reconnectService.BeginMatch(msg.ReconnectToken);

            _matchState.IsWaitingImageActive.Value = false;

            _matchState.Hand.Clear();
            _matchState.EnemyCardAmount.Value = 0;

            var dealt = Mathf.Max(msg.CardsInHand.Length, msg.EnemyCardAmount);

            for (var i = 0; i < dealt; i++)
            {
                var cardData = i < msg.CardsInHand.Length ? msg.CardsInHand[i] : default;
                var hasOwnCard = i < msg.CardsInHand.Length;
                var enemyAmount = Mathf.Min(i + 1, msg.EnemyCardAmount);

                _coordinator.Enqueue(() => DealBeat(cardData, hasOwnCard, enemyAmount),
                    CardAnimConfig.DrawBeatDuration);
            }
        }

        private void DealBeat(CardData cardData, bool hasOwnCard, int enemyAmount)
        {
            if (hasOwnCard)
            {
                var instance = _instances.GetOrCreate(cardData.DefinitionId, cardData.InstanceId);

                if (instance != null)
                {
                    _matchState.Hand.Add(instance);
                    _matchState.CardDrawn.OnNext(Unit.Default);
                }
            }

            if (enemyAmount <= _matchState.EnemyCardAmount.Value) return;

            _matchState.EnemyCardAmount.Value = enemyAmount;
            _matchState.EnemyCardDrawn.OnNext(Unit.Default);
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
                yield return null;

                var hits = ConsumePendingHits(card.Id, sequence, out var batch);

                if (hits.Length > 0)
                {
                    _matchState.DamageDealt.OnNext(hits);
                    yield return new WaitForSeconds(CardAnimConfig.ProjectileDuration);
                    yield return PlayStatesOf(batch);
                }

                place();
                yield return new WaitForSeconds(CardAnimConfig.PlayFlightDuration);
            }
        }

        private IEnumerator PlayHits(Guid sourceId, int sequence)
        {
            var hits = ConsumePendingHits(sourceId, sequence, out _);
            if (hits.Length == 0) yield break;

            _matchState.DamageDealt.OnNext(hits);
            yield return new WaitForSeconds(CardAnimConfig.ProjectileDuration);
        }

        private IEnumerator PlayStatesOf(int batch)
        {
            while (true)
            {
                var index = _pendingStates.FindIndex(s => s.Batch == batch);
                if (index < 0) yield break;

                var message = _pendingStates[index].Message;
                _pendingStates.RemoveAt(index);

                yield return ApplyUnitStates(message);
            }
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

        private DamageInstance[] ConsumePendingHits(Guid sourceId, int sequence, out int batch)
        {
            var id = sourceId.ToString();

            bool Matches(PendingHit h) => h.PlaySequence == sequence && h.Hit.SourceInstanceId == id;

            batch = 0;

            if (!_pendingHits.Any(Matches)) return Array.Empty<DamageInstance>();

            var earliest = _pendingHits.Where(Matches).Min(h => h.Batch);
            batch = earliest;

            bool MatchesBatch(PendingHit h) => Matches(h) && h.Batch == earliest;

            var hits = _pendingHits.Where(MatchesBatch).Select(h => h.Hit).ToArray();
            _pendingHits.RemoveAll(MatchesBatch);

            return hits;
        }


        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}