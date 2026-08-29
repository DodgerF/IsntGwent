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

        private static readonly TimeSpan PendingTimeout = TimeSpan.FromSeconds(5);

        private readonly CompositeDisposable _disposables = new();
        private readonly SerialDisposable _pendingTimeout = new();
        private readonly List<PendingHit> _pendingHits = new();
        private readonly List<PendingLink> _pendingLinks = new();
        private readonly List<PendingState> _pendingStates = new();

        private int _playSequence;
        private int _hitBatch;
        private int _linkBatch;
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

        private readonly struct PendingLink
        {
            public readonly UnitLinkData Link;
            public readonly int PlaySequence;
            public readonly int Batch;

            public PendingLink(UnitLinkData link, int playSequence, int batch)
            {
                Link = link;
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
            
            _handler.OnEnemyPassed
                .Subscribe(_ => _matchState.IsEnemyPassed.Value = true)
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
                    var boardRow = e.Row as BoardRowView;

                    _handler.SendPlayCard(
                        e.Card.Id.ToString(),
                        boardRow != null ? boardRow.BoardRow : RowType.None,
                        boardRow != null && !boardRow.OwnSide,
                        e.SlotIndex,
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

            _handler.OnUnitLinks
                .Subscribe(msg =>
                {
                    var batch = ++_linkBatch;

                    foreach (var link in msg.Links)
                        _pendingLinks.Add(new PendingLink(link, _playSequence, batch));

                    _coordinator.EnqueueRoutine(() => FlushPendingLinks(batch));
                })
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

            _handler.OnDeckSync
                .Subscribe(msg => _coordinator.Enqueue(() => OnDeckSync(msg)))
                .AddTo(_disposables);

            _handler.OnPendingPlay
                .Subscribe(msg => _coordinator.Enqueue(() => OnPendingPlay(msg)))
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
                .Subscribe(_ => _reconnectService.EndSeat())
                .AddTo(_disposables);

            _connectionService.IsConnectionLost
                .Where(v => v)
                .First()
                .Subscribe(_ => OnConnectionLost())
                .AddTo(_disposables);

            _reconnectService.IsReconnecting
                .Where(v => !v)
                .Skip(1)
                .Where(_ => !_reconnectService.HasSeat)
                .Subscribe(_ => EndByConnectionLost())
                .AddTo(_disposables);
        }

        private void OnConnectionLost()
        {
            ClearAnimations();
            ClearPending();

            if (_reconnectService.HasSeat && !_matchState.IsGameEnded.Value)
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

            _matchState.IsRestoring = true;

            try
            {
                SyncCollection(_matchState.Hand, msg.CardsInHand);
                _matchState.EnemyCardAmount.Value = msg.EnemyCardAmount;

                SyncCollection(_matchState.OwnDeck, msg.OwnDeck);
                _matchState.EnemyDeckCount.Value = msg.EnemyDeckCount;

                SyncRow(_matchState.OwnMeleeRow, msg.OwnMeleeRow);
                SyncRow(_matchState.OwnRangedRow, msg.OwnRangedRow);
                SyncRow(_matchState.EnemyMeleeRow, msg.EnemyMeleeRow);
                SyncRow(_matchState.EnemyRangedRow, msg.EnemyRangedRow);
                SyncCollection(_matchState.OwnGraveyard, msg.OwnGraveyard);
                SyncCollection(_matchState.EnemyGraveyard, msg.EnemyGraveyard);
                SyncWeather(msg.OwnRowStatus, msg.EnemyRowStatus);

                _matchState.IsPendingMine.Value = msg.IsPendingMine;
                SyncPendingPlays(msg.PendingPlays);
            }
            finally
            {
                _matchState.IsRestoring = false;
            }

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

            _matchState.IsEnemyPassed.Value = msg.IsEnemyPassed;
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
                    _matchState.ActionTimedOut.OnNext(Unit.Default);
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
            _pendingLinks.Clear();
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
                unit.Armor.Value = data.Armor;

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
                unit.ResetToBase();
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
            _matchState.IsEnemyPassed.Value = false;
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
            SyncRow(_matchState.OwnMeleeRow, msg.OwnMeleeRow);
            SyncRow(_matchState.OwnRangedRow, msg.OwnRangedRow);
            SyncRow(_matchState.EnemyMeleeRow, msg.EnemyMeleeRow);
            SyncRow(_matchState.EnemyRangedRow, msg.EnemyRangedRow);
            SyncCollection(_matchState.OwnGraveyard, msg.OwnGraveyard);
            SyncCollection(_matchState.EnemyGraveyard, msg.EnemyGraveyard);
            SyncWeather(msg.OwnRowStatus, msg.EnemyRowStatus);
        }

        private void SyncWeather(RowStatusData[] own, RowStatusData[] enemy)
        {
            ApplyWeather(own, _matchState.OwnMeleeWeather, _matchState.OwnRangedWeather);
            ApplyWeather(enemy, _matchState.EnemyMeleeWeather, _matchState.EnemyRangedWeather);
        }

        private static void ApplyWeather(RowStatusData[] data, RowWeatherState melee, RowWeatherState ranged)
        {
            melee.Clear();
            ranged.Clear();

            if (data == null) return;

            foreach (var status in data)
            {
                if (status.Row == RowType.Melee) melee.Apply(status);
                else if (status.Row == RowType.Ranged) ranged.Apply(status);
            }
        }

        private void SyncRow(BoardRowState row, CardData[] data)
        {
            for (var i = 0; i < row.Slots.Length; i++)
            {
                var cardData = i < data.Length ? data[i] : default;

                if (cardData.IsEmpty)
                {
                    if (row.Slots[i] != null)
                        row.Remove(row.Slots[i]);
                    continue;
                }

                var instance = _instances.GetOrCreate(cardData.DefinitionId, cardData.InstanceId);
                if (instance == null) continue;

                if (instance is UnitInstance unit)
                {
                    unit.CurrentPower.Value = cardData.CurrentPower;
                    unit.Armor.Value = cardData.Armor;
                }

                if (row.Slots[i] != instance)
                    row.Place(i, instance);
            }
        }

        private void OnPendingPlay(PendingPlayMessage msg)
        {
            ClearPending();

            _matchState.IsPendingMine.Value = msg.IsMine;
            SyncPendingPlays(msg.Cards);

            _selectionService.TryArmPendingPlay();
        }

        private void SyncPendingPlays(CardData[] data)
        {
            var incoming = new List<CardInstance>();

            foreach (var cardData in data ?? Array.Empty<CardData>())
            {
                var instance = _instances.GetOrCreate(cardData.DefinitionId, cardData.InstanceId);
                if (instance == null) continue;

                if (instance is UnitInstance unit)
                {
                    unit.CurrentPower.Value = cardData.CurrentPower;
                    unit.Armor.Value = cardData.Armor;
                }

                incoming.Add(instance);
            }

            for (var i = _matchState.PendingPlays.Count - 1; i >= 0; i--)
            {
                if (incoming.Contains(_matchState.PendingPlays[i])) continue;

                _matchState.PendingPlays.RemoveAt(i);
            }

            foreach (var card in incoming)
            {
                if (_matchState.PendingPlays.Contains(card)) continue;

                _matchState.PendingPlays.Add(card);

                if (!_matchState.IsRestoring)
                    _matchState.PendingPlayGranted.OnNext(card);
            }
        }

        private void OnDeckSync(DeckSyncMessage msg)
        {
            SyncCollection(_matchState.OwnDeck, msg.OwnDeck);
            _matchState.EnemyDeckCount.Value = msg.EnemyDeckCount;
        }

        private void SyncCollection(ReactiveCollection<CardInstance> collection, CardData[] data)
        {
            var incoming = new List<(CardInstance Card, CardData Data)>();

            foreach (var cardData in data)
            {
                var instance = _instances.GetOrCreate(cardData.DefinitionId, cardData.InstanceId);
                if (instance == null) continue;

                incoming.Add((instance, cardData));
            }

            for (var i = collection.Count - 1; i >= 0; i--)
            {
                if (incoming.Any(entry => entry.Card == collection[i])) continue;

                collection.RemoveAt(i);
            }

            foreach (var entry in incoming)
            {
                if (!collection.Contains(entry.Card))
                    collection.Add(entry.Card);

                if (entry.Card is not UnitInstance unit) continue;

                unit.CurrentPower.Value = entry.Data.CurrentPower;
                unit.Armor.Value = entry.Data.Armor;
            }
        }

        private void OnRedrawStarted(RedrawStartedMessage msg)
        {
            ClearPending();
            _matchState.IsRedrawReady.Value = false;
            _matchState.RedrawsLeft.Value = msg.RedrawsLeft;
            _matchState.IsRedrawPhase.Value = true;

            if (msg.RedrawsLeft <= 0)
                _matchState.RedrawReadyRequested.OnNext(Unit.Default);
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
                    row.Place(msg.SlotIndex, card);
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
            {
                unit.CurrentPower.Value = msg.CurrentPower;
                unit.Armor.Value = msg.Armor;
            }

            var sequence = ++_playSequence;

            _coordinator.EnqueueRoutine(() => PlayCardBeat(instance, sequence, isEnemy: true, () =>
            {
                if (instance is UnitInstance)
                {
                    var row = msg.Row == RowType.Melee
                        ? _matchState.EnemyMeleeRow
                        : _matchState.EnemyRangedRow;
                    row.Place(msg.SlotIndex, instance);
                }
                else if (instance is SpellInstance spell)
                {
                    _matchState.EnemyGraveyard.Add(spell);
                }
            },
            () => _matchState.EnemyCardAmount.Value = msg.CardAmount));
        }

        private IEnumerator PlayCardBeat(
            CardInstance card,
            int sequence,
            bool isEnemy,
            Action place,
            Action onStage = null)
        {
            _matchState.CardStaged.OnNext(card);
            onStage?.Invoke();

            if (isEnemy && !_matchState.PendingPlays.Contains(card))
                yield return new WaitForSeconds(CardAnimConfig.EnemyStageDuration);

            if (card is UnitInstance)
            {
                place();
                yield return new WaitForSeconds(CardAnimConfig.PlayFlightDuration);
                yield return PlayLinks(card.Id, sequence);
                yield return PlayHits(card.Id, sequence);
            }
            else
            {
                yield return null;

                yield return PlayLinks(card.Id, sequence);

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

        private IEnumerator PlayLinks(Guid sourceId, int sequence)
        {
            var links = ConsumePendingLinks(sourceId, sequence);
            if (links.Length == 0) yield break;

            _matchState.UnitsLinked.OnNext(links);
            yield return new WaitForSeconds(CardAnimConfig.UnitLinkBeatDuration);
        }

        private IEnumerator FlushPendingLinks(int batch)
        {
            var links = _pendingLinks
                .Where(l => l.Batch == batch)
                .Select(l => l.Link)
                .ToArray();

            if (links.Length == 0) yield break;

            _pendingLinks.RemoveAll(l => l.Batch == batch);

            _matchState.UnitsLinked.OnNext(links);

            yield return new WaitForSeconds(CardAnimConfig.UnitLinkBeatDuration);
        }

        private UnitLinkData[] ConsumePendingLinks(Guid sourceId, int sequence)
        {
            var id = sourceId.ToString();

            bool Matches(PendingLink l) => l.PlaySequence == sequence && l.Link.SourceInstanceId == id;

            if (!_pendingLinks.Any(Matches)) return Array.Empty<UnitLinkData>();

            var earliest = _pendingLinks.Where(Matches).Min(l => l.Batch);

            bool MatchesBatch(PendingLink l) => Matches(l) && l.Batch == earliest;

            var links = _pendingLinks.Where(MatchesBatch).Select(l => l.Link).ToArray();
            _pendingLinks.RemoveAll(MatchesBatch);

            return links;
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