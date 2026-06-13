using System;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;
using ReadyMessage = IsntGwent.Scripts.Messages.ReadyMessage;

namespace IsntGwent.Scripts.Match
{
    public class MatchClientHandler : IInitializable, IDisposable
    {
        public readonly Subject<Unit> OnGameStarted = new();
        public void Initialize()
        {
            if (NetworkClient.active)
            {
                NetworkClient.RegisterHandler<GameStartedMessage>(_ =>OnGameStarted.OnNext(Unit.Default));
            }
        }

        public void SendReadyMessage()
        {
            NetworkClient.Send(new ReadyMessage());
        }
        
        public void Dispose()
        {
            NetworkClient.UnregisterHandler<GameStartedMessage>();
        }
    }
}