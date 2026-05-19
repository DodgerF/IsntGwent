using System;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Effects;

namespace IsntGwent.Scripts.Cards
{
    public class EffectFactory
    {
        public ICardEffect Create(
            EffectDefinition definition)
        {
            switch (definition)
            {
                case DealDamageEffectDefinition:
                    return new DealDamageEffect();

                default:
                    throw new Exception(
                        $"Unknown effect: {definition.GetType()}"
                    );
            }
        }
    }
}