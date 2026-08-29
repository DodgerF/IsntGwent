using System;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;
using ReadyMessage = IsntGwent.Scripts.Messages.ReadyMessage;

namespace IsntGwent.Scripts.Match.Client
{
    public class MatchClientHandler : IInitializable, IDisposable
    {
        public readonly Subject<GameStartedMessage> OnGameStarted = new();
        public readonly Subject<TurnChangedMessage> OnTurnChanged = new();
        public readonly Subject<OwnCardPlayedMessage> OnOwnCardPlayed = new();
        public readonly Subject<EnemyCardPlayedMessage> OnEnemyCardPlayed = new();
        public readonly Subject<CardRemovedFromHandMessage> OnCardRemovedFromHand = new();
        public readonly Subject<PowerUpdatedMessage> OnPowerUpdated = new();
        public readonly Subject<CardDrawnMessage> OnCardDrawn = new();
        public readonly Subject<EnemyCardDrawnMessage> OnEnemyCardDrawn = new();
        public readonly Subject<BoardSyncMessage> OnBoardSync = new();
        public readonly Subject<RoundEndedMessage> OnRoundEnded = new();
        public readonly Subject<GameEndedMessage> OnGameEnded = new();
        public readonly Subject<HpChangedMessage> OnHpChanged = new();
        public readonly Subject<UnitsStateChangedMessage> OnUnitsStateChanged = new();
        public readonly Subject<DamageDealtMessage> OnDamageDealt = new();
        public readonly Subject<UnitLinksMessage> OnUnitLinks = new();
        public readonly Subject<EnemyPassedMessage> OnEnemyPassed = new();
        public readonly Subject<EnemyDisconnectedMessage> OnEnemyDisconnected = new();
        public readonly Subject<GiveUpMessage> OnGiveUp = new();
        public readonly Subject<RedrawStartedMessage> OnRedrawStarted = new();
        public readonly Subject<CardRedrawnMessage> OnCardRedrawn = new();
        public readonly Subject<RedrawEndedMessage> OnRedrawEnded = new();
        public readonly Subject<MatchSnapshotMessage> OnSnapshot = new();
        public readonly Subject<OpponentReconnectingMessage> OnOpponentReconnecting = new();
        public readonly Subject<PendingPlayMessage> OnPendingPlay = new();
        public readonly Subject<DeckSyncMessage> OnDeckSync = new();

        public void Initialize()
        {
            if (!NetworkClient.active) return;
            
            NetworkClient.RegisterHandler<GameStartedMessage>(msg => OnGameStarted.OnNext(msg));
            NetworkClient.RegisterHandler<TurnChangedMessage>(msg => OnTurnChanged.OnNext(msg));
            NetworkClient.RegisterHandler<OwnCardPlayedMessage>(msg => OnOwnCardPlayed.OnNext(msg));
            NetworkClient.RegisterHandler<EnemyCardPlayedMessage>(msg => OnEnemyCardPlayed.OnNext(msg));
            NetworkClient.RegisterHandler<CardRemovedFromHandMessage>(msg => OnCardRemovedFromHand.OnNext(msg));
            NetworkClient.RegisterHandler<PowerUpdatedMessage>(msg => OnPowerUpdated.OnNext(msg));
            NetworkClient.RegisterHandler<CardDrawnMessage>(msg => OnCardDrawn.OnNext(msg));
            NetworkClient.RegisterHandler<EnemyCardDrawnMessage>(msg => OnEnemyCardDrawn.OnNext(msg));
            NetworkClient.RegisterHandler<BoardSyncMessage>(msg => OnBoardSync.OnNext(msg));
            NetworkClient.RegisterHandler<RoundEndedMessage>(msg => OnRoundEnded.OnNext(msg));
            NetworkClient.RegisterHandler<GameEndedMessage>(msg => OnGameEnded.OnNext(msg));
            NetworkClient.RegisterHandler<HpChangedMessage>(msg => OnHpChanged.OnNext(msg));
            NetworkClient.RegisterHandler<UnitsStateChangedMessage>(msg => OnUnitsStateChanged.OnNext(msg));
            NetworkClient.RegisterHandler<DamageDealtMessage>(msg => OnDamageDealt.OnNext(msg));
            NetworkClient.RegisterHandler<UnitLinksMessage>(msg => OnUnitLinks.OnNext(msg));
            NetworkClient.RegisterHandler<EnemyPassedMessage>(msg => OnEnemyPassed.OnNext(msg));
            NetworkClient.RegisterHandler<EnemyDisconnectedMessage>(msg => OnEnemyDisconnected.OnNext(msg));
            NetworkClient.RegisterHandler<GiveUpMessage>(msg => OnGiveUp.OnNext(msg));
            NetworkClient.RegisterHandler<RedrawStartedMessage>(msg => OnRedrawStarted.OnNext(msg));
            NetworkClient.RegisterHandler<CardRedrawnMessage>(msg => OnCardRedrawn.OnNext(msg));
            NetworkClient.RegisterHandler<RedrawEndedMessage>(msg => OnRedrawEnded.OnNext(msg));
            NetworkClient.RegisterHandler<MatchSnapshotMessage>(msg => OnSnapshot.OnNext(msg));
            NetworkClient.RegisterHandler<OpponentReconnectingMessage>(msg => OnOpponentReconnecting.OnNext(msg));
            NetworkClient.RegisterHandler<PendingPlayMessage>(msg => OnPendingPlay.OnNext(msg));
            NetworkClient.RegisterHandler<DeckSyncMessage>(msg => OnDeckSync.OnNext(msg));
        }
        public void SendReadyMessage()
        {
            NetworkClient.Send(new ReadyMessage());
        }

        public void SendPass()
        {
            NetworkClient.Send(new PassMessage());
        }
        
        public void SendPlayCard(string cardInstanceId, RowType row, bool enemyRow, int slotIndex, string[] targetIds)
        {
            NetworkClient.Send(new PlayCardMessage
            {
                CardInstanceId = cardInstanceId,
                Row = row,
                EnemyRow = enemyRow,
                SlotIndex = slotIndex,
                TargetIds = targetIds
            });
        }

        public void SendLeave()
        {
            NetworkClient.Send(new LeaveMessage());
        }

        public void SendRedrawCard(string cardInstanceId)
        {
            NetworkClient.Send(new RedrawCardMessage { CardInstanceId = cardInstanceId });
        }

        public void SendRedrawReady()
        {
            NetworkClient.Send(new RedrawReadyMessage());
        }

        public void Dispose()
        {
            NetworkClient.UnregisterHandler<GameStartedMessage>();
            NetworkClient.UnregisterHandler<TurnChangedMessage>();
            NetworkClient.UnregisterHandler<OwnCardPlayedMessage>();
            NetworkClient.UnregisterHandler<EnemyCardPlayedMessage>();
            NetworkClient.UnregisterHandler<CardRemovedFromHandMessage>();
            NetworkClient.UnregisterHandler<PowerUpdatedMessage>();
            NetworkClient.UnregisterHandler<CardDrawnMessage>();
            NetworkClient.UnregisterHandler<EnemyCardDrawnMessage>();
            NetworkClient.UnregisterHandler<BoardSyncMessage>();
            NetworkClient.UnregisterHandler<RoundEndedMessage>();
            NetworkClient.UnregisterHandler<GameEndedMessage>();
            NetworkClient.UnregisterHandler<HpChangedMessage>();
            NetworkClient.UnregisterHandler<UnitsStateChangedMessage>();
            NetworkClient.UnregisterHandler<DamageDealtMessage>();
            NetworkClient.UnregisterHandler<UnitLinksMessage>();
            NetworkClient.UnregisterHandler<EnemyPassedMessage>();
            NetworkClient.UnregisterHandler<EnemyDisconnectedMessage>();
            NetworkClient.UnregisterHandler<GiveUpMessage>();
            NetworkClient.UnregisterHandler<RedrawStartedMessage>();
            NetworkClient.UnregisterHandler<CardRedrawnMessage>();
            NetworkClient.UnregisterHandler<RedrawEndedMessage>();
            NetworkClient.UnregisterHandler<MatchSnapshotMessage>();
            NetworkClient.UnregisterHandler<OpponentReconnectingMessage>();
            NetworkClient.UnregisterHandler<PendingPlayMessage>();
            NetworkClient.UnregisterHandler<DeckSyncMessage>();
        }
    }
}