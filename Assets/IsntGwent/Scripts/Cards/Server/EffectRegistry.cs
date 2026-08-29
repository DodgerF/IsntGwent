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
            _effects[typeof(AimedTargetingDefinition)] = new AimedTargetingEffect();
            _effects[typeof(WeakestTargetingDefinition)] = new WeakestTargetingEffect();
            _effects[typeof(StrongestTargetingDefinition)] = new StrongestTargetingEffect();
            _effects[typeof(NeighborTargetingDefinition)] = new NeighborTargetingEffect();
            _effects[typeof(SlotTargetingDefinition)] = new SlotTargetingEffect();
            _effects[typeof(LineTargetingDefinition)] = new LineTargetingEffect();
            _effects[typeof(RowTargetingDefinition)] = new RowTargetingEffect();
            _effects[typeof(EventTargetingDefinition)] = new EventTargetingEffect();
            _effects[typeof(DealDamageEffectDefinition)] = new DealDamageEffect();
            _effects[typeof(BuffPowerDefinition)] = new BuffPowerEffect();
            _effects[typeof(DevourDefinition)] = new DevourEffect();
            _effects[typeof(DestroyEffectDefinition)] = new DestroyEffect();
            _effects[typeof(SummonFromDeckDefinition)] = new SummonFromDeckEffect();
            _effects[typeof(SummonTokenDefinition)] = new SummonTokenEffect();
            _effects[typeof(MoveEffectDefinition)] = new MoveEffect();
            _effects[typeof(GainArmorDefinition)] = new GainArmorEffect();
            _effects[typeof(HealDefinition)] = new HealEffect();
            _effects[typeof(PurgeGraveyardDefinition)] = new PurgeGraveyardEffect();
            _effects[typeof(CastFromDeckDefinition)] = new CastFromDeckEffect();
            _effects[typeof(ApplyWeatherDefinition)] = new ApplyWeatherEffect();
        }

        public ICardEffect Get(EffectDefinition definition)
        {
            return _effects[definition.GetType()];
        }
    }
}