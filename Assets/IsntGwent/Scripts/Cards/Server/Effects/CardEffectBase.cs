using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public abstract class CardEffectBase : ICardEffect
    {
        public virtual bool NeedsManualTargets(EffectDefinition definition) => false;
        public virtual int ManualTargetCount(EffectDefinition definition) => 0;

        public virtual bool CanTrigger(EffectContext context) => true;

        public virtual List<UnitInstance> ResolveTargets(EffectContext context)
            => context.ManualTargets;

        public abstract void Execute(EffectContext context);
    }
}
