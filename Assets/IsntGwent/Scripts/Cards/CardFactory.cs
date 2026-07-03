using System;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Cards
{
    public static class CardFactory
    {
        public static CardInstance Create(CardDefinition definition)
        {
            return definition switch
            {
                UnitDefinition unitDefinition => new UnitInstance(unitDefinition),
                SpellDefinition spellDefinition => new SpellInstance(spellDefinition),
                _ => throw new NotSupportedException(
                    $"Unknown card type {definition.GetType().Name}")
            };
        }
    }
}