namespace IsntGwent.Scripts.Cards.Definitions
{
    public enum SlotCondition
    {
        None,
        NeighborsEmpty,
        HasNeighbor,
        OppositeOccupied,
        OppositeEmpty,
    }

    public abstract class EffectDefinition
    {
        public EffectTrigger Trigger;
        public RowType RequiredRow;
        public SlotCondition SlotCondition;
        public EffectCondition Condition;
    }

}
