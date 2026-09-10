using System;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;
using ReadyMessage = IsntGwent.Scripts.Messages.ReadyMessage;

namespace IsntGwent.Scripts.Lobby.Server
{
    public class ServerHandler : IInitializable, IDisposable
    {
        [Inject] private readonly SeatRegistry _seats;

        public readonly Subject<(NetworkConnectionToClient conn, FindMatchMessage msg)> OnFindMatch = new();
        public readonly Subject<NetworkConnectionToClient> OnCancelSearch = new();
        public readonly Subject<(NetworkConnectionToClient conn, CreatePrivateRoomMessage msg)> OnCreatePrivateRoom = new();
        public readonly Subject<(NetworkConnectionToClient conn, JoinByCodeMessage msg)> OnJoinByCode = new();
        public readonly Subject<Seat> OnReady = new();
        public readonly Subject<(NetworkConnectionToClient conn, ReconnectRequestMessage msg)> OnReconnect = new();
        public readonly Subject<NetworkConnectionToClient> OnStartTutorial = new();
        public readonly Subject<(Seat seat, TutorialActionMessage msg)> OnTutorialAction = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            NetworkServer.RegisterHandler<FindMatchMessage>((conn, msg) => OnFindMatch.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<CancelSearchMessage>((conn, _) => OnCancelSearch.OnNext(conn));
            NetworkServer.RegisterHandler<CreatePrivateRoomMessage>((conn, msg) => OnCreatePrivateRoom.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<JoinByCodeMessage>((conn, msg) => OnJoinByCode.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<ReadyMessage>((conn, _) => Publish(OnReady, conn));
            NetworkServer.RegisterHandler<ReconnectRequestMessage>((conn, msg) => OnReconnect.OnNext((conn, msg)));
            NetworkServer.RegisterHandler<StartTutorialMessage>((conn, _) => OnStartTutorial.OnNext(conn));
            NetworkServer.RegisterHandler<TutorialActionMessage>(PublishTutorialAction);
        }

        private void PublishTutorialAction(NetworkConnectionToClient conn, TutorialActionMessage msg)
        {
            var seat = _seats.Resolve(conn);
            if (seat == null) return;

            OnTutorialAction.OnNext((seat, msg));
        }

        private void Publish(Subject<Seat> subject, NetworkConnectionToClient conn)
        {
            var seat = _seats.Resolve(conn);
            if (seat == null) return;

            subject.OnNext(seat);
        }

        public void Dispose()
        {
            NetworkServer.UnregisterHandler<FindMatchMessage>();
            NetworkServer.UnregisterHandler<CancelSearchMessage>();
            NetworkServer.UnregisterHandler<CreatePrivateRoomMessage>();
            NetworkServer.UnregisterHandler<JoinByCodeMessage>();
            NetworkServer.UnregisterHandler<ReadyMessage>();
            NetworkServer.UnregisterHandler<ReconnectRequestMessage>();
            NetworkServer.UnregisterHandler<StartTutorialMessage>();
            NetworkServer.UnregisterHandler<TutorialActionMessage>();
        }
    }
}
