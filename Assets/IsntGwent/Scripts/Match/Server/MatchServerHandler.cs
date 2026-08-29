using System;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Server;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class MatchServerHandler : IInitializable, IDisposable
    {
        [Inject] private readonly SeatRegistry _seats;

        public readonly Subject<(Seat seat, PlayCardMessage msg)> OnPlayCard = new();
        public readonly Subject<Seat> OnPass = new();
        public readonly Subject<Seat> OnLeave = new();
        public readonly Subject<(Seat seat, RedrawCardMessage msg)> OnRedrawCard = new();
        public readonly Subject<Seat> OnRedrawReady = new();

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            NetworkServer.RegisterHandler<PlayCardMessage>((conn, msg) => Publish(OnPlayCard, conn, msg));
            NetworkServer.RegisterHandler<PassMessage>((conn, _) => Publish(OnPass, conn));
            NetworkServer.RegisterHandler<LeaveMessage>((conn, _) => Publish(OnLeave, conn));
            NetworkServer.RegisterHandler<RedrawCardMessage>((conn, msg) => Publish(OnRedrawCard, conn, msg));
            NetworkServer.RegisterHandler<RedrawReadyMessage>((conn, _) => Publish(OnRedrawReady, conn));
        }

        private void Publish(Subject<Seat> subject, NetworkConnectionToClient conn)
        {
            var seat = _seats.Resolve(conn);
            if (seat == null) return;

            subject.OnNext(seat);
        }

        private void Publish<T>(Subject<(Seat seat, T msg)> subject, NetworkConnectionToClient conn, T message)
        {
            var seat = _seats.Resolve(conn);
            if (seat == null) return;

            subject.OnNext((seat, message));
        }

        public void Dispose()
        {
            NetworkServer.UnregisterHandler<PlayCardMessage>();
            NetworkServer.UnregisterHandler<PassMessage>();
            NetworkServer.UnregisterHandler<LeaveMessage>();
            NetworkServer.UnregisterHandler<RedrawCardMessage>();
            NetworkServer.UnregisterHandler<RedrawReadyMessage>();
        }
    }
}
