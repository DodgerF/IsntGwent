using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;

namespace IsntGwent.Scripts.Cards.Server
{
    public class EffectRegistry
    {
        private readonly Dictionary<Type, ICardEffect> _effects = new();

        public EffectRegistry()
        {
            _effects[typeof(ManualTargetingDefinition)] = new ManualTargetingEffect();
            _effects[typeof(WeakestTargetingDefinition)] = new WeakestTargetingEffect();
            _effects[typeof(DealDamageEffectDefinition)] = new DealDamageEffect();
        }

        public ICardEffect Get(EffectDefinition definition)
        {
            return _effects[definition.GetType()];
        }
    }
}