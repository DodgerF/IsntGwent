using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;
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

            var boardOwner = IsTraitor(card) ? context.GetOpponent(player) : player;

            if (!CanPlaceCard(boardOwner, card, row, slotIndex))
                return;

            if (!_cardResolver.CanPlay(context, boardOwner, card, selectedIds, row, slotIndex))
                return;

            if (!PlaceCard(context, boardOwner, card, row, slotIndex))
                return;

            context.Journal?.Play(context, player, boardOwner, card, row, slotIndex, selectedIds, fromPending);

            if (fromPending)
            {
                player.PendingPlays.Remove(card);
                _notifier.NotifyCardPlayed(context, boardOwner, card, row, slotIndex);
            }
            else
            {
                player.Hand.Remove(card);
                _notifier.NotifyCardPlayed(context, boardOwner, card, row, slotIndex);
                _notifier.NotifyCardRemovedFromHand(player, card);
            }

            var pause = _cardResolver.PlayCard(context, boardOwner, card, row, slotIndex, selectedIds, enemyRow);
            _boardSync.Sync(context);

            if (pause != null)
            {
                BeginAim(context, player, boardOwner, card, row, slotIndex, enemyRow, fromPending, pause);
                return;
            }

            FinishPlay(context, player, boardOwner, card, row, slotIndex, fromPending);
        }

        public void ContinueAim(GameContext context, Player player, List<string> targetIds)
        {
            var pending = context.PendingAim;
            if (pending == null || pending.Caster != player) return;

            context.PendingAim = null;

            var chosen = FilterAimTargets(pending, targetIds);
            pending.TargetIds.Clear();
            pending.TargetIds.AddRange(chosen);

            context.Journal?.Aim(context, pending.Caster, pending.Card, pending.TargetIds);

            var pause = _cardResolver.ContinuePlay(context, pending);
            _boardSync.Sync(context);

            if (pause != null)
            {
                Suspend(context, pending, pause);
                return;
            }

            FinishPlay(context, pending.Caster, pending.BoardOwner, pending.Card, pending.Row, pending.Slot,
                pending.FromPending);
        }

        private static IEnumerable<string> FilterAimTargets(PendingAim pending, List<string> targetIds)
        {
            if (targetIds == null) return new List<string>();

            var definition = pending.Card.Definition.Effects[pending.EffectIndex] as ManualTargetingDefinition;
            var limit = definition?.Count ?? 0;

            return targetIds.Where(pending.Pool.Contains).Distinct().Take(limit).ToList();
        }

        private void BeginAim(GameContext context, Player caster, Player boardOwner, CardInstance card,
            RowType row, int slotIndex, bool enemyRow, bool fromPending, AimPause pause)
        {
            var pending = new PendingAim
            {
                Caster = caster,
                BoardOwner = boardOwner,
                Card = card,
                Row = row,
                Slot = slotIndex,
                EnemyRow = enemyRow,
                FromPending = fromPending,
            };

            Suspend(context, pending, pause);
        }

        private void Suspend(GameContext context, PendingAim pending, AimPause pause)
        {
            pending.EffectIndex = pause.EffectIndex;
            pending.DestroyedPower = pause.DestroyedPower;
            pending.KilledCount = pause.KilledCount;
            pending.Pool.Clear();
            pending.Pool.AddRange(pause.Pool);

            context.PendingAim = pending;

            if (context.FlushBoardDirty())
                _boardSync.SyncBoard(context);

            context.Journal?.AimRequest(context, pending.Caster, pending.Card, pending.Pool);

            _notifier.NotifyAimRequest(pending.Caster, pending.Card, pending.Pool);
        }

        private void FinishPlay(GameContext context, Player player, Player boardOwner, CardInstance card,
            RowType row, int slotIndex, bool fromPending)
        {
            if (DiedInWrongRow(card))
            {
                BoardSyncService.MoveToGraveyard(context, (UnitInstance)card);
                ((UnitInstance)card).ResetToBase();
                _boardSync.SyncBoard(context);
            }

            context.Publish(new CardPlayed(boardOwner, card, row, slotIndex));
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

        public static bool IsTraitor(CardInstance card)
        {
            return card is UnitInstance unit && unit.UnitDefinition.Traitor;
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
