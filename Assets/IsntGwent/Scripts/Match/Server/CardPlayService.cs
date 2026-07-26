using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Server;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class CardPlayService
    {
        [Inject] private readonly MatchServerNotifier _notifier;
        [Inject] private readonly CardResolver _cardResolver;
        [Inject] private readonly BoardSyncService _boardSync;
        [Inject] private readonly TurnService _turnService;

        public void PlayCard(GameContext context, Player player, CardInstance card,
            RowType row, List<string> selectedIds)
        {
            if (!player.Hand.Contains(card))
                return;

            if (!_cardResolver.CanPlay(context, player, card.Definition, selectedIds))
                return;

            if (!PlaceCard(context, player, card, row))
                return;

            player.Hand.Remove(card);
            _notifier.NotifyCardPlayed(context, player, card, row);
            _notifier.NotifyCardRemovedFromHand(player, card);

            _cardResolver.PlayCard(context, player, card, selectedIds);
            _boardSync.Sync(context);

            context.Publish(new CardPlayed(player, card, row));
            _boardSync.Sync(context);
            if (player.Hand.Count == 0)
            {
                _turnService.PassTurn(context, player);
            }
            else
            {
                _turnService.ChangeTurn(context);
            }
        }

        private static bool PlaceCard(GameContext context, Player player, CardInstance card, RowType row)
        {
            if (card is UnitInstance unit)
            {
                if (unit.UnitDefinition.Row != row) return false;

                player.GetRow(row).Add(unit);
                context.OnUnitAddedToRow(unit);
            }
            else
            {
                player.Graveyard.Add(card);
            }

            return true;
        }
    }
}
