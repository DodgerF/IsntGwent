using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards.Effects
{
    public class DealDamageEffect : CardEffectBase
    {
        public override bool NeedsManualTargets(EffectDefinition def) => true;
        public override int ManualTargetCount(EffectDefinition def) => 
            ((DealDamageEffectDefinition)def).TargetCount;

        public override void Execute(GameContext context, CardInstance source,
            List<UnitInstance> targets, EffectDefinition definition)
        {
            var def = (DealDamageEffectDefinition)definition;
            foreach (var target in targets)
                target.GetDamage(def.Amount);
        }
    }

    public class DealDamageEffectDefinition : EffectDefinition
    {
        public int Amount;
        public int TargetCount;
    }
}