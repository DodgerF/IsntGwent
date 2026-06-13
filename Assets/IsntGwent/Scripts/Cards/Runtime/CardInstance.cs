using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Effects;
using IsntGwent.Scripts.Match;
using Mirror;

namespace IsntGwent.Scripts.Cards.Runtime
{
    public abstract class CardInstance
    {
        public CardDefinition Definition { get; }
        public NetworkConnectionToClient Owner;
        public List<ICardEffect> Effects { get; } = new();

        protected CardInstance(CardDefinition definition)
        {
            Definition = definition;
        }
    }
}