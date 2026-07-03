using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Effects;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards
{
    public class CardResolver
    {
        private readonly EffectRegistry _effectRegistry;

        public CardResolver(EffectRegistry effectRegistry)
        {
            _effectRegistry = effectRegistry;
        }
        
        public void PlayCard(GameContext context, Player caster, CardInstance card, List<string> selectedIds)
        {
            var manualTargets = ResolveManualTargets(context, selectedIds);
            List<UnitInstance> currentTargets = new();

            foreach (var effectDef in card.Definition.Effects)
            {
                if (effectDef.Trigger != EffectTrigger.OnPlay) continue;

                var effect = _effectRegistry.Get(effectDef);
                
                if (effect is TargetingEffect)
                {
                    currentTargets = effect.ResolveTargets(context, caster, effectDef, manualTargets);
                    continue;
                }
                
                effect.Execute(context, card, currentTargets, effectDef);
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