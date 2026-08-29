using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public class AimedTargetingEffect : ManualTargetingEffect
    {
        public override List<UnitInstance> GetPool(EffectContext context)
        {
            var pool = BuildPool(context);

            if (context.Source is UnitInstance source)
                pool.Remove(source);

            return pool;
        }
    }

    public class AimedTargetingDefinition : ManualTargetingDefinition
    {
    }
}
