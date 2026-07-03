using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Cards.Runtime
{
    public class SpellInstance : CardInstance
    {
        public SpellDefinition SpellDefinition => (SpellDefinition)Definition;

        public SpellInstance(SpellDefinition definition)
            : base(definition)
        {
        }
    }
}