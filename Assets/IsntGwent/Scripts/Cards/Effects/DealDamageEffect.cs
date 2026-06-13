using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards.Effects
{
    public class DealDamageEffect : ICardEffect
    {
        public void Execute(
            GameContext context, 
            CardInstance source, 
            List<UnitInstance> targets, 
            EffectDefinition definition)
        {
            var damageDefinition =
                (DealDamageEffectDefinition)definition;
            
            foreach (var target in targets)
            {
                target.GetDamage(damageDefinition.Amount);
            }
        }
    }
}