using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Server;

namespace IsntGwent.Scripts.Cards.Server
{
    public class CardResolver
    {
        private readonly EffectRegistry _effectRegistry;

        public CardResolver(EffectRegistry effectRegistry)
        {
            _effectRegistry = effectRegistry;
        }

        public bool PlayCard(GameContext context, Player caster, CardInstance card, List<string> selectedIds)
        {
            var manualTargets = ResolveManualTargets(context, selectedIds);

            if (!ValidateManualTargets(context, caster, card.Definition, manualTargets))
                return false;

            RunEffects(context, caster, card, EffectTrigger.OnPlay, null, manualTargets);
            return true;
        }
        
        public bool CanPlay(GameContext context, Player caster, CardDefinition definition, List<string> selectedIds)
        {
            var manualTargets = ResolveManualTargets(context, selectedIds);
            return ValidateManualTargets(context, caster, definition, manualTargets);
        }
        
        private bool ValidateManualTargets(GameContext context, Player caster, CardDefinition definition,
            List<UnitInstance> manualTargets)
        {
            foreach (var effectDef in definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;
                if (effectDef is not ManualTargetingDefinition manualDef) continue;

                var effect = (ManualTargetingEffect)_effectRegistry.Get(effectDef);
                var pool = effect.GetPool(new EffectContext
                {
                    Game = context,
                    Owner = caster,
                    Definition = effectDef,
                });

                var poolIds = new HashSet<Guid>(pool.Select(u => u.Id));
                if (manualTargets.Any(t => !poolIds.Contains(t.Id)))
                    return false;

                var maxAvailable = Math.Min(manualDef.Count, pool.Count);
                var expected = manualDef.AllowPartial ? maxAvailable : manualDef.Count;

                return manualTargets.Count == expected;
            }

            return true;
        }
        
        public void RunEffects(GameContext context, Player owner, CardInstance card,
            EffectTrigger trigger, IGameEvent gameEvent, List<UnitInstance> manualTargets)
        {
            var effectContext = new EffectContext
            {
                Game = context,
                Source = card,
                Owner = owner,
                Event = gameEvent,
                ManualTargets = manualTargets ?? new List<UnitInstance>(),
            };

            foreach (var effectDef in card.Definition.Effects)
            {
                if (effectDef.Trigger != trigger) continue;

                effectContext.Definition = effectDef;

                var effect = _effectRegistry.Get(effectDef);
                if (!effect.CanTrigger(effectContext)) continue;

                if (effect is TargetingEffect)
                {
                    effectContext.Targets = effect.ResolveTargets(effectContext);
                    continue;
                }

                effect.Execute(effectContext);
            }
        }

        private List<UnitInstance> ResolveManualTargets(GameContext context, List<string> selectedIds)
        {
            if (selectedIds == null || selectedIds.Count == 0) return new List<UnitInstance>();

            return context.Player1.MeleeRow
                .Concat(context.Player1.RangedRow)
                .Concat(context.Player2.MeleeRow)
                .Concat(context.Player2.RangedRow)
                .Where(u => selectedIds.Contains(u.Id.ToString()))
                .ToList();
        }

        public bool NeedsManualTargets(CardDefinition definition, out int count)
        {
            foreach (var effectDef in definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;

                var effect = _effectRegistry.Get(effectDef);
                if (effect.NeedsManualTargets(effectDef))
                {
                    count = effect.ManualTargetCount(effectDef);
                    return true;
                }
            }

            count = 0;
            return false;
        }

        public List<string> GetTargetPool(
            CardDefinition definition,
            IEnumerable<UnitInstance> ownUnits,
            IEnumerable<UnitInstance> enemyUnits)
        {
            foreach (var effectDef in definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;

                if (effectDef is not TargetingEffectDefinition targetingDef) continue;

                var result = new List<string>();

                if (targetingDef.IncludeEnemies)
                    result.AddRange(enemyUnits.Select(u => u.Id.ToString()));
                if (targetingDef.IncludeAllies)
                    result.AddRange(ownUnits.Select(u => u.Id.ToString()));

                return result;
            }

            return new List<string>();
        }
    }
}
