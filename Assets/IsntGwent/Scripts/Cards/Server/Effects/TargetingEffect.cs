using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public abstract class TargetingEffect : CardEffectBase
    {
        public abstract List<UnitInstance> GetPool(EffectContext context);

        public override void Execute(EffectContext context) { }
        
        protected static List<UnitInstance> BuildPool(EffectContext context)
        {
            var definition = (TargetingEffectDefinition)context.Definition;
            var result = new List<UnitInstance>();
            var opponent = context.Game.GetOpponent(context.Owner);

            if (definition.IncludeEnemies)
            {
                result.AddRange(opponent.MeleeRow);
                result.AddRange(opponent.RangedRow);
            }

            if (definition.IncludeAllies)
            {
                result.AddRange(context.Owner.MeleeRow);
                result.AddRange(context.Owner.RangedRow);
            }

            return result;
        }
    }

    public abstract class TargetingEffectDefinition : EffectDefinition
    {
        public bool IncludeAllies;
        public bool IncludeEnemies;
    }
}
