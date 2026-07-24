using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Lobby.Server;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class GameControllerServer : IInitializable, IDisposable
    {
        [Inject] private readonly MatchServerHandler _handler;
        [Inject] private readonly MatchServerNotifier _notifier;
        [Inject] private readonly LobbyManager _lobbyManager;
        [Inject] private readonly CardPlayService _cardPlayService;
        [Inject] private readonly TurnService _turnService;
        [Inject] private readonly TriggeredEffectDispatcher _dispatcher;

        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            _handler.OnPlayCard
                .Subscribe(t => OnPlayCard(t.conn, t.msg))
                .AddTo(_disposables);

            _handler.OnPass
                .Subscribe(OnPass)
                .AddTo(_disposables);

            _handler.OnLeave
                .Subscribe(OnLeave)
                .AddTo(_disposables);
        }
        
        private void OnLeave(NetworkConnectionToClient conn)
        {
            var context = _lobbyManager.GetGameContext(conn);
            var player = context?.GetPlayer(conn);

            if (player != null)
                EndGameBySurrender(context, context.GetOpponent(player), notifyLoser: false);

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

            _cardPlayService.PlayCard(context, player, card, msg.Row, selectedIds);
        }

        private void OnPass(NetworkConnectionToClient conn)
        {
            var context = _lobbyManager.GetGameContext(conn);
            if (context == null) return;

            var player = context.GetPlayer(conn);
            if (context.CurrentPlayer != player) return;
            if (player.IsPassed) return;

            _turnService.PassTurn(context, player);
        }

        public void StartGame(GameContext context)
        {
            _dispatcher.Attach(context);

            DeckService.Shuffle(context.Player1.Deck);
            DeckService.Shuffle(context.Player2.Deck);

            DeckService.DrawCards(context.Player1, 10);
            DeckService.DrawCards(context.Player2, 10);

            var rnd = UnityEngine.Random.Range(0, 2);
            context.CurrentPlayer = rnd == 0 ? context.Player1 : context.Player2;

            _notifier.NotifyGameStarted(context);

            context.Publish(new TurnStarted(context.CurrentPlayer));
        }

        public void EndGameBySurrender(GameContext context, Player winner, bool notifyLoser = true)
        {
            if (context.GameEnded.Value) return;

            _notifier.NotifyGiveUp(winner, notifyLoser ? context.GetOpponent(winner) : null);

            context.GameEnded.Value = true;
        }

        public void EndGameByDisconnect(GameContext context, Player winner)
        {
            Debug.Log("EndGameByDisconnect");
            if (context.GameEnded.Value) return;

            _notifier.NotifyEnemyDisconnected(winner);

            context.GameEnded.Value = true;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
