using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server.Effects;

namespace IsntGwent.Scripts.Cards.Runtime
{
    public abstract class CardInstance
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public void SetId(Guid id) => Id = id;
        public CardDefinition Definition { get; }
        public List<ICardEffect> Effects { get; } = new();

        protected CardInstance(CardDefinition definition)
        {
            Definition = definition;
        }
    }
}