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
                    break;

                case RoundEnded:
                    RunOnBoard(context, gameEvent, _ => EffectTrigger.OnRoundEnd);
                    break;
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

                    RunCard(context, player, unit, trigger.Value, gameEvent);
                }
            }
        }

        private void RunCard(GameContext context, Player owner, CardInstance card,
            EffectTrigger trigger, IGameEvent gameEvent)
        {
            if (!HasTrigger(card, trigger)) return;

            _cardResolver.RunEffects(context, owner, card, trigger, gameEvent, null);
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
