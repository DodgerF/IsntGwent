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
            RowType row, int slotIndex, List<string> selectedIds, bool enemyRow = false)
        {
            var fromPending = card == player.NextPendingPlay;

            if (!fromPending && (player.PendingPlays.Count > 0 || !player.Hand.Contains(card)))
                return;

            if (!CanPlaceCard(player, card, row, slotIndex))
                return;

            if (!_cardResolver.CanPlay(context, player, card, selectedIds, row, slotIndex))
                return;

            if (!PlaceCard(context, player, card, row, slotIndex))
                return;

            if (fromPending)
            {
                player.PendingPlays.Remove(card);
                _notifier.NotifyCardPlayed(context, player, card, row, slotIndex);
            }
            else
            {
                player.Hand.Remove(card);
                _notifier.NotifyCardPlayed(context, player, card, row, slotIndex);
                _notifier.NotifyCardRemovedFromHand(player, card);
            }

            _cardResolver.PlayCard(context, player, card, row, slotIndex, selectedIds, enemyRow);
            _boardSync.Sync(context);

            if (DiedInWrongRow(card))
            {
                BoardSyncService.MoveToGraveyard(context, (UnitInstance)card);
                ((UnitInstance)card).ResetToBase();
                _boardSync.SyncBoard(context);
            }

            context.Publish(new CardPlayed(player, card, row, slotIndex));
            _boardSync.Sync(context);

            if (context.FlushBoardDirty())
                _boardSync.SyncBoard(context);

            var pendingChanged = fromPending | DiscardUnplayablePending(context, player);

            if (context.PendingPlayNotice == player)
            {
                context.PendingPlayNotice = null;
                pendingChanged = true;
            }

            if (pendingChanged)
                _notifier.NotifyPendingPlay(context, player);

            FlushPendingNotice(context);

            if (player.PendingPlays.Count > 0)
                return;

            if (player.Hand.Count == 0)
            {
                _turnService.PassTurn(context, player);
            }
            else
            {
                _turnService.ChangeTurn(context);
            }

            FlushPendingNotice(context);
        }

        private void FlushPendingNotice(GameContext context)
        {
            var waiting = context.PendingPlayNotice;
            if (waiting == null) return;

            context.PendingPlayNotice = null;

            DiscardUnplayablePending(context, waiting);
            _notifier.NotifyPendingPlay(context, waiting);
        }

        private bool DiscardUnplayablePending(GameContext context, Player player)
        {
            var discarded = false;

            while (player.NextPendingPlay is UnitInstance unit && player.FirstFreeSlot() == null)
            {
                player.PendingPlays.Remove(unit);
                player.Graveyard.Add(unit);
                discarded = true;
            }

            if (discarded)
                _boardSync.SyncBoard(context);

            return discarded;
        }

        private static bool DiedInWrongRow(CardInstance card)
        {
            return card is UnitInstance unit
                   && unit.UnitDefinition.DiesInWrongRow
                   && unit.RowType != unit.UnitDefinition.RequiredRow;
        }

        public static bool IsRowValid(CardInstance card, RowType row)
        {
            if (card is not UnitInstance) return true;

            return row is RowType.Melee or RowType.Ranged;
        }

        public static bool IsSlotFree(Player player, CardInstance card, RowType row, int slotIndex)
        {
            if (card is not UnitInstance) return true;

            return player.GetRow(row).IsFree(slotIndex);
        }

        private static bool CanPlaceCard(Player player, CardInstance card, RowType row, int slotIndex)
            => IsRowValid(card, row) && IsSlotFree(player, card, row, slotIndex);

        private static bool PlaceCard(GameContext context, Player player, CardInstance card, RowType row, int slotIndex)
        {
            if (card is UnitInstance unit)
            {
                if (!player.GetRow(row).TryPlace(unit, slotIndex))
                    return false;

                unit.RowType = row;
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
