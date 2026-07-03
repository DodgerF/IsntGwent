using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct CardData : NetworkMessage
    {
        public string InstanceId;
        public string DefinitionId;
        public CardType Type;
    }
    
    public static class CardDataFactory
    {
        public static CardData Create(CardInstance card) => new CardData
        {
            InstanceId = card.Id.ToString(),
            DefinitionId = card.Definition.Id,
            Type = card.Definition.Type,
        };
    }
}