using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards.Effects
{
    
    public class ManualTargetingEffect : TargetingEffect
    {
        public override bool NeedsManualTargets(EffectDefinition def) => true;
        public override int ManualTargetCount(EffectDefinition def) => ((ManualTargetingDefinition)def).Count;

        public override List<UnitInstance> GetPool(GameContext context, Player caster, EffectDefinition def)
        {
            var d = (ManualTargetingDefinition)def;
            var result = new List<UnitInstance>();
            var opponent = context.GetOpponent(caster);

            if (d.IncludeEnemies)
            {
                result.AddRange(opponent.MeleeRow);
                result.AddRange(opponent.RangedRow);
            }
            if (d.IncludeAllies)
            {
                result.AddRange(caster.MeleeRow);
                result.AddRange(caster.RangedRow);
            }

            return result;
        }

        public override List<UnitInstance> ResolveTargets(GameContext context, Player caster,
            EffectDefinition definition, List<UnitInstance> manualTargets)
        {
            return manualTargets;
        }
    }
    public class ManualTargetingDefinition : TargetingEffectDefinition
    {
        public int Count;
        public bool AllowPartial;
    }
}