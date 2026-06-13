using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match;

namespace IsntGwent.Scripts.Cards.Effects
{
    public interface ICardEffect
    {
        void Execute(
        GameContext context,
        CardInstance source,
        List<UnitInstance> targets,
        EffectDefinition definition);
    }
}