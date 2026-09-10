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
        public int CurrentPower;
        public int BasePower;
        public int Armor;

        public bool IsEmpty => string.IsNullOrEmpty(InstanceId);
    }

    public static class CardDataFactory
    {
        public static CardData Create(CardInstance card) => new CardData
        {
            InstanceId = card.Id.ToString(),
            DefinitionId = card.Definition.Id,
            Type = card.Definition.Type,
            CurrentPower = card is UnitInstance unit ? unit.CurrentPower.Value : 0,
            BasePower = card is UnitInstance based ? based.BasePower.Value : 0,
            Armor = card is UnitInstance armored ? armored.Armor.Value : 0,
        };
    }
}