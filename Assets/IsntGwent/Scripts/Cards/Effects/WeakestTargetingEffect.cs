using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards.Effects
{
    public class WeakestTargetingEffect : TargetingEffect
    {
        public override bool NeedsManualTargets(EffectDefinition def) => false;

        public override List<UnitInstance> GetPool(GameContext context, Player caster, EffectDefinition def)
        {
            var d = (WeakestTargetingDefinition)def;
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
            EffectDefinition def, List<UnitInstance> manualTargets)
        {
            var pool = GetPool(context, caster, def);
            if (pool.Count == 0) return new List<UnitInstance>();

            var min = pool.Min(u => u.CurrentPower.Value);
            return pool.Where(u => u.CurrentPower.Value == min).ToList();
        }
    }
    public class WeakestTargetingDefinition : TargetingEffectDefinition
    {
    }
}