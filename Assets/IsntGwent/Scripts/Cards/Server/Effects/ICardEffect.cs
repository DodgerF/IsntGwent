using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public interface ICardEffect
    {
        bool NeedsManualTargets(EffectDefinition definition);
        int ManualTargetCount(EffectDefinition definition);
        
        bool CanTrigger(EffectContext context);

        List<UnitInstance> ResolveTargets(EffectContext context);

        void Execute(EffectContext context);
    }
}
