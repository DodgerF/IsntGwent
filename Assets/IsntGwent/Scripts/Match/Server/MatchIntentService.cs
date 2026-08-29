using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Server;
using IsntGwent.Scripts.Messages;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public enum IntentError
    {
        None,
        MatchNotFound,
        MatchPaused,
        NotYourTurn,
        WrongPhase,
        CardNotInHand,
        IllegalRow,
        IllegalSlot,
        IllegalTargets,
    }

    public class MatchIntentService
    {
        [Inject] private readonly LobbyManager _lobbyManager;
        [Inject] private readonly CardPlayService _cardPlayService;
        [Inject] private readonly TurnService _turnService;
        [Inject] private readonly RedrawService _redrawService;
        [Inject] private readonly CardResolver _cardResolver;

        public IntentError PlayCard(Seat seat, PlayCardMessage msg)
        {
            var error = ActivePlayer(seat, out var context, out var player);
            if (error != IntentError.None) return error;

            if (context.IsRedrawPhase) return IntentError.WrongPhase;
            if (context.CurrentPlayer != player) return IntentError.NotYourTurn;

            var card = FindPlayable(player, msg.CardInstanceId);
            if (card == null) return IntentError.CardNotInHand;

            if (!CardPlayService.IsRowValid(card, msg.Row)) return IntentError.IllegalRow;
            if (!CardPlayService.IsSlotFree(player, card, msg.Row, msg.SlotIndex)) return IntentError.IllegalSlot;

            var selectedIds = msg.TargetIds?.ToList() ?? new List<string>();

            if (!_cardResolver.CanPlay(context, player, card, selectedIds, msg.Row, msg.SlotIndex))
                return IntentError.IllegalTargets;

            _cardPlayService.PlayCard(context, player, card, msg.Row, msg.SlotIndex, selectedIds, msg.EnemyRow);

            return IntentError.None;
        }

        public IntentError Pass(Seat seat)
        {
            var error = ActivePlayer(seat, out var context, out var player);
            if (error != IntentError.None) return error;

            if (context.IsRedrawPhase) return IntentError.WrongPhase;
            if (context.CurrentPlayer != player) return IntentError.NotYourTurn;
            if (player.IsPassed) return IntentError.WrongPhase;
            if (player.PendingPlays.Count > 0) return IntentError.WrongPhase;

            _turnService.PassTurn(context, player);

            return IntentError.None;
        }

        public IntentError Redraw(Seat seat, string cardInstanceId)
        {
            var error = ActivePlayer(seat, out var context, out var player);
            if (error != IntentError.None) return error;

            if (!context.IsRedrawPhase) return IntentError.WrongPhase;
            if (player.IsRedrawReady || player.RedrawsLeft <= 0) return IntentError.WrongPhase;

            if (player.Hand.All(c => c.Id.ToString() != cardInstanceId))
                return IntentError.CardNotInHand;

            _redrawService.Redraw(context, player, cardInstanceId);

            return IntentError.None;
        }

        public IntentError RedrawReady(Seat seat)
        {
            var error = ActivePlayer(seat, out var context, out var player);
            if (error != IntentError.None) return error;

            if (!context.IsRedrawPhase) return IntentError.WrongPhase;

            _redrawService.SetReady(context, player);

            return IntentError.None;
        }

        private IntentError ActivePlayer(Seat seat, out GameContext context, out Player player)
        {
            context = _lobbyManager.GetGameContext(seat);
            player = context?.GetPlayer(seat);

            if (context == null || player == null) return IntentError.MatchNotFound;
            if (context.IsPaused) return IntentError.MatchPaused;

            return IntentError.None;
        }

        private static CardInstance FindPlayable(Player player, string cardInstanceId)
        {
            var pending = player.NextPendingPlay;

            if (pending != null)
                return pending.Id.ToString() == cardInstanceId ? pending : null;

            return player.Hand.FirstOrDefault(c => c.Id.ToString() == cardInstanceId);
        }
    }
}
