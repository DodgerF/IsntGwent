using System;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;
using ReadyMessage = IsntGwent.Scripts.Messages.ReadyMessage;

namespace IsntGwent.Scripts.Match
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
            
        }
        public void SendReadyMessage()
        {
            NetworkClient.Send(new ReadyMessage());
        }

        public void SendPass()
        {
            NetworkClient.Send(new PassMessage());
        }
        
        public void SendPlayCard(string cardInstanceId, RowType row)
        {
            NetworkClient.Send(new PlayCardMessage
            {
                CardInstanceId = cardInstanceId, 
                Row = row,
            });
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
        }
    }
}