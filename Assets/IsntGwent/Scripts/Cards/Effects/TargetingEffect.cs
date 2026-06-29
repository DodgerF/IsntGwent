using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards.Effects
{
    public abstract class TargetingEffect : CardEffectBase
    {
        public abstract List<UnitInstance> GetPool(GameContext context, Player caster, EffectDefinition def);
    
        public override void Execute(GameContext context, CardInstance source,
            List<UnitInstance> targets, EffectDefinition definition) { }
    }

    public abstract class TargetingEffectDefinition : EffectDefinition
    {
        public bool IncludeAllies;
        public bool IncludeEnemies;
    }
}