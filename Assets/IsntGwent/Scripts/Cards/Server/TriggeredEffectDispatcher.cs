using System;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Cards.Server
{
    public class TriggeredEffectDispatcher
    {
        [Inject] private readonly CardResolver _cardResolver;
        [Inject] private readonly BoardSyncService _boardSync;

        public void Attach(GameContext context)
        {
            context.AddDisposable(
                context.Events.Stream.Subscribe(gameEvent => Dispatch(context, gameEvent)));
        }

        private void Dispatch(GameContext context, IGameEvent gameEvent)
        {
            switch (gameEvent)
            {
                case UnitDied died when died.Owner != null:
                    RunCard(context, died.Owner, died.Unit, EffectTrigger.OnDeath, gameEvent);
                    RunKiller(context, died);
                    RunOnBoard(context, gameEvent, owner => owner == died.Owner
                        ? EffectTrigger.OnAllyDied
                        : (EffectTrigger?)null);
                    break;

                case UnitDevoured devoured when devoured.Owner != null:
                    RunConsumed(context, devoured);
                    RunOnBoard(context, gameEvent, owner => owner == devoured.Owner
                        ? EffectTrigger.OnAllyDevoured
                        : (EffectTrigger?)null);
                    break;

                case UnitSummoned summoned when summoned.Owner != null:
                    RunOnBoard(context, gameEvent, owner => owner == summoned.Owner
                        ? EffectTrigger.OnAllySummoned
                        : (EffectTrigger?)null);
                    break;

                case TurnStarted turnStarted:
                    RunOnBoard(context, gameEvent, owner => owner == turnStarted.Player
                        ? EffectTrigger.OnTurnStart
                        : EffectTrigger.OnEnemyTurnStart);
                    break;

                case TurnEnded turnEnded:
                    RunOnBoard(context, gameEvent, owner => owner == turnEnded.Player
                        ? EffectTrigger.OnTurnEnd
                        : null);
                    break;
                
                case CardPlayed cardPlayed:
                    RunOnBoard(context, gameEvent, _ => EffectTrigger.OnCardPlayed, cardPlayed.Card);
                    RunOnBoard(context, gameEvent, owner => owner != cardPlayed.Owner
                        ? EffectTrigger.OnEnemyCardPlayed
                        : (EffectTrigger?)null, cardPlayed.Card);
                    break;

                case UnitMoved moved:
                    RunOnBoard(context, gameEvent, _ => EffectTrigger.OnUnitMoved, moved.Unit);
                    break;

                case RoundEnded:
                    RunOnBoard(context, gameEvent, _ => EffectTrigger.OnRoundEnd);
                    break;
            }
        }

        private void RunConsumed(GameContext context, UnitDevoured devoured)
        {
            var consumed = devoured.Consumed;
            if (consumed == null) return;

            foreach (var player in new[] { context.Player1, context.Player2 })
            {
                if (!player.MeleeRow.Contains(consumed) && !player.RangedRow.Contains(consumed)) continue;

                RunCard(context, player, consumed, EffectTrigger.OnDevoured, devoured);
                return;
            }
        }

        private void RunKiller(GameContext context, UnitDied died)
        {
            if (died.Killer is not UnitInstance killer) return;
            if (killer == died.Unit) return;

            foreach (var player in new[] { context.Player1, context.Player2 })
            {
                if (!player.MeleeRow.Contains(killer) && !player.RangedRow.Contains(killer)) continue;

                RunCard(context, player, killer, EffectTrigger.OnKill, died);
                return;
            }
        }

        private void RunOnBoard(GameContext context, IGameEvent gameEvent,
            Func<Player, EffectTrigger?> triggerForOwner, CardInstance skip = null)
        {
            foreach (var player in new[] { context.Player1, context.Player2 })
            {
                var trigger = triggerForOwner(player);
                if (trigger == null) continue;
                
                var units = player.MeleeRow.Concat(player.RangedRow).ToList();

                foreach (var unit in units)
                {
                    if (unit == skip) continue;
                    if (!player.MeleeRow.Contains(unit) && !player.RangedRow.Contains(unit)) continue;

                    RunCard(context, player, unit, trigger.Value, gameEvent);
                }
            }
        }

        private void RunCard(GameContext context, Player owner, CardInstance card,
            EffectTrigger trigger, IGameEvent gameEvent)
        {
            if (!HasTrigger(card, trigger)) return;

            _cardResolver.RunEffects(context, owner, card, trigger, gameEvent, null);
            _boardSync.Sync(context);

            if (context.FlushBoardDirty())
                _boardSync.SyncBoard(context);
        }

        private static bool HasTrigger(CardInstance card, EffectTrigger trigger)
        {
            var effects = card.Definition.Effects;

            for (var i = 0; i < effects.Count; i++)
            {
                if (effects[i].Trigger == trigger) return true;
            }

            return false;
        }
    }
}
