using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards.Effects
{
    public class DealDamageToWeakestEffect : CardEffectBase
    {
        public override bool NeedsManualTargets(EffectDefinition def) => false;

        public override List<UnitInstance> ResolveTargets(GameContext context, Player caster,
            EffectDefinition definition, List<UnitInstance> manualTargets)
        {
            var allUnits = context.Player1.MeleeRow
                .Concat(context.Player1.RangedRow)
                .Concat(context.Player2.MeleeRow)
                .Concat(context.Player2.RangedRow)
                .ToList();

            if (allUnits.Count == 0) return new List<UnitInstance>();

            var minPower = allUnits.Min(u => u.CurrentPower.Value);

            return allUnits
                .Where(u => u.CurrentPower.Value == minPower)
                .ToList();
        }

        public override void Execute(GameContext context, CardInstance source,
            List<UnitInstance> targets, EffectDefinition definition)
        {
            var def = (DealDamageToWeakestEffectDefinition)definition;
            foreach (var target in targets)
                target.GetDamage(def.Amount);
        }
    }

    public class DealDamageToWeakestEffectDefinition : EffectDefinition
    {
        public int Amount;
    }
}