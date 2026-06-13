using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards
{
    public class CardResolver
    {
        private readonly EffectRegistry _effectRegistry;

        public CardResolver(EffectRegistry effectRegistry)
        {
            _effectRegistry = effectRegistry;
        }
        
        public void PlayCard(
            GameContext context,
            CardInstance card,
            List<UnitInstance> targets)
        {
            foreach (var effectDefinition in card.Definition.Effects)
            {
                if (effectDefinition.Trigger != "OnPlay")
                    continue;

                var effect =
                    _effectRegistry.Get(effectDefinition);

                effect.Execute(
                    context,
                    card,
                    targets,
                    effectDefinition);
            }
        }
    }
}