using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Diagnostics;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class BoardSyncService
    {
        private const int MaxDeathCascadeIterations = 16;

        [Inject] private readonly MatchServerNotifier _notifier;
        
        public void Sync(GameContext context)
        {
            ResolveDeaths(context);
            _notifier.NotifyPower(context);
        }

        public void SyncBoard(GameContext context)
        {
            _notifier.NotifyBoardSync(context);
            _notifier.NotifyDecks(context);
        }

        private void ResolveDeaths(GameContext context)
        {
            for (var i = 0; i < MaxDeathCascadeIterations; i++)
            {
                var damage = context.FlushDamageRecords();
                var changed = context.FlushChangedUnits();

                _notifier.NotifyUnitLinks(context, context.FlushLinkRecords());
                _notifier.NotifyDamageDealt(context, damage);

                if (changed.Count == 0) return;

                _notifier.NotifyUnitStates(context, changed);
                context.Journal?.States(context, changed);

                var dead = changed.Where(u => u.CurrentPower.Value <= 0).ToList();
                if (dead.Count == 0) return;

                foreach (var unit in dead)
                {
                    var slot = context.FindSlot(unit);
                    var (left, right) = FindNeighbors(context, unit);
                    var row = slot?.Row ?? unit.RowType;
                    var index = slot?.Index ?? -1;
                    var killer = LastHitOn(damage, unit);

                    var owner = MoveToGraveyard(context, unit);
                    unit.ResetToBase();

                    context.Publish(new UnitDied(unit, owner, left, right, row, index, killer));
                }

                ApplySlotTakeovers(context);
            }

            Log.Error(LogTag.Match,
                $"каскад смертей не сошёлся за {MaxDeathCascadeIterations} итераций " +
                $"(match {context.MatchId}) — похоже на карты, убивающие друг друга по кругу");
        }
        
        private static void ApplySlotTakeovers(GameContext context)
        {
            foreach (var pair in context.FlushSlotTakeovers())
            {
                var unit = pair.Key;
                var to = pair.Value;

                if (!to.IsEmpty) continue;

                var from = context.FindSlot(unit);
                if (from == null || from == to || from.Owner != to.Owner) continue;

                from.Unit = null;
                to.Unit = unit;
                unit.RowType = to.Row;

                context.MarkBoardDirty();
                context.Publish(new UnitMoved(unit, to.Owner, from.Row, from.Index, to.Row, to.Index));
            }
        }

        private static CardInstance LastHitOn(IReadOnlyList<DamageRecord> damage, UnitInstance unit)
        {
            for (var i = damage.Count - 1; i >= 0; i--)
            {
                if (damage[i].Target != unit || damage[i].Source == null) continue;
                if (damage[i].Kind == DamageKind.Weather) continue;

                return damage[i].Source;
            }

            return unit.LastAttacker;
        }

        public static (UnitInstance Left, UnitInstance Right) FindNeighbors(GameContext context, UnitInstance unit)
        {
            var slot = context.FindSlot(unit);
            if (slot == null) return (null, null);

            return (slot.Left?.Unit, slot.Right?.Unit);
        }

        public static Player MoveToGraveyard(GameContext context, UnitInstance unit)
        {
            foreach (var player in new[] { context.Player1, context.Player2 })
            {
                if (player.MeleeRow.Remove(unit) || player.RangedRow.Remove(unit))
                {
                    context.OnUnitRemovedFromRow(unit);
                    player.Graveyard.Add(unit);
                    return player;
                }
            }

            return null;
        }

        public static void MoveAllToGraveyard(GameContext context, Player player)
        {
            foreach (var row in new[] { player.MeleeRow, player.RangedRow })
            {
                foreach (var unit in row)
                {
                    context.OnUnitRemovedFromRow(unit);
                    unit.ResetToBase();
                    player.Graveyard.Add(unit);
                }

                row.Clear();
            }
        }
    }
}
