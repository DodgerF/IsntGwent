using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Cards.Server.Effects
{
    public static class SlotConditions
    {
        public static bool IsMet(EffectContext context, EffectDefinition definition)
        {
            if (definition.SlotCondition == SlotCondition.None) return true;

            var slot = context.SourceSlot;
            if (slot == null) return false;

            var hasNeighbor = slot.Left?.Unit != null || slot.Right?.Unit != null;

            return definition.SlotCondition switch
            {
                SlotCondition.NeighborsEmpty => !hasNeighbor,
                SlotCondition.HasNeighbor => hasNeighbor,
                SlotCondition.OppositeOccupied => slot.Opposite?.Unit != null,
                SlotCondition.OppositeEmpty => slot.Opposite?.Unit == null,
                _ => true
            };
        }
    }
}
