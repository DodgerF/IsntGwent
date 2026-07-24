using System.Linq;
using IsntGwent.Scripts.Cards.Runtime;
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
        }

        private void ResolveDeaths(GameContext context)
        {
            for (var i = 0; i < MaxDeathCascadeIterations; i++)
            {
                var changed = context.FlushChangedUnits();
                if (changed.Count == 0) return;

                _notifier.NotifyUnitStates(context, changed);

                var dead = changed.Where(u => u.CurrentPower.Value <= 0).ToList();
                if (dead.Count == 0) return;

                foreach (var unit in dead)
                {
                    var owner = MoveToGraveyard(context, unit);
                    unit.CurrentPower.Value = unit.UnitDefinition.Power;
                    
                    context.Publish(new UnitDied(unit, owner));
                }
            }

            Debug.LogError($"BoardSyncService: каскад смертей не сошёлся за {MaxDeathCascadeIterations} " +
                           "итераций — похоже на карты, убивающие друг друга по кругу");
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
            foreach (var unit in player.MeleeRow)
            {
                context.OnUnitRemovedFromRow(unit);
                player.Graveyard.Add(unit);
            }

            foreach (var unit in player.RangedRow)
            {
                context.OnUnitRemovedFromRow(unit);
                player.Graveyard.Add(unit);
            }

            player.MeleeRow.Clear();
            player.RangedRow.Clear();
        }
    }
}
