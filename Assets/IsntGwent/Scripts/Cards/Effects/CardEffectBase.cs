using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards.Effects
{
    public abstract class CardEffectBase : ICardEffect
    {
        public virtual bool NeedsManualTargets(EffectDefinition definition) => false;
        public virtual int ManualTargetCount(EffectDefinition definition) => 0;

        public virtual List<UnitInstance> ResolveTargets(GameContext context, Player caster,
            EffectDefinition definition, List<UnitInstance> manualTargets)
            => manualTargets;

        public abstract void Execute(GameContext context, CardInstance source,
            List<UnitInstance> targets, EffectDefinition definition);
    }
}