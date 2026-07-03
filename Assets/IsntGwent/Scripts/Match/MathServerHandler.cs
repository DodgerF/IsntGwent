using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Server;
using Mirror;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    public class MathServerHandler : IInitializable, IDisposable
    {
        [Inject] private readonly LobbyManager _lobbyManager;
        [Inject] private readonly CardResolver _cardResolver;
        
        public void Initialize()
        {
            if (!NetworkServer.active) return;
            
            NetworkServer.RegisterHandler<PlayCardMessage>(OnPlayCard);
            NetworkServer.RegisterHandler<PassMessage>(OnPass);
            NetworkServer.RegisterHandler<LeaveMessage>(OnLeave);
        }

        private void OnLeave(NetworkConnectionToClient conn, LeaveMessage msg)
        {
            _lobbyManager.LeaveLobby(conn);
        }
        
        private void OnPlayCard(NetworkConnectionToClient conn, PlayCardMessage msg)
        {
            var context = _lobbyManager.GetGameContext(conn);
            if (context == null) return;
            
            var player = context.GetPlayer(conn);
            if (context.CurrentPlayer != player) return;

            var card = player.Hand.FirstOrDefault(c => c.Id.ToString() == msg.CardInstanceId);
            if (card == null) return;

            var selectedIds = msg.TargetIds?.ToList() ?? new List<string>();

            GameControllerServer.PlayCard(context, player, card, msg.Row, selectedIds, _cardResolver);
        }
        
        private void OnPass(NetworkConnectionToClient conn, PassMessage msg)
        {
            var context = _lobbyManager.GetGameContext(conn);
            if (context == null) return;

            var player = context.GetPlayer(conn);
            if (context.CurrentPlayer != player) return;
            if (player.IsPassed) return;

            GameControllerServer.PassTurn(context, player);
            GameControllerServer.SyncPower(context);
        }


        public void Dispose()
        {
            NetworkServer.UnregisterHandler<PlayCardMessage>();
            NetworkServer.UnregisterHandler<PassMessage>();
            NetworkServer.UnregisterHandler<LeaveMessage>();
        }
    }
}