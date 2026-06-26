using System;
using System.Linq;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Server;
using Mirror;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    public class MathServerHandler : IInitializable, IDisposable
    {
        [Inject] private readonly LobbyManager _lobbyManager;
        
        public void Initialize()
        {
            if (!NetworkServer.active) return;
            
            NetworkServer.RegisterHandler<PlayCardMessage>(OnPlayCard);
            NetworkServer.RegisterHandler<PassMessage>(OnPass);
        }
        
        private void OnPlayCard(NetworkConnectionToClient conn, PlayCardMessage msg)
        {
            var context = _lobbyManager.GetGameContext(conn);
            if (context == null) return;
            
            var player = context.GetPlayer(conn);
            if (context.CurrentPlayer != player) return;

            var card = player.Hand.FirstOrDefault(c => c.Id.ToString() == msg.CardInstanceId);
            if (card == null) return;

            GameControllerServer.PlayCard(context, player, card, msg.Row);
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
        }
    }
}