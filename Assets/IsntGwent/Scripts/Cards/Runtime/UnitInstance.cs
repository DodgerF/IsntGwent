using IsntGwent.Scripts.Cards.Definitions;
using UniRx;

namespace IsntGwent.Scripts.Cards.Runtime
{
    public class UnitInstance : CardInstance
    {
        public RowType RowType;
        
        public UnitDefinition UnitDefinition => (UnitDefinition)Definition;
        
        public readonly ReactiveProperty<int> CurrentPower = new();
        public UnitInstance(UnitDefinition definition) : base(definition)
        {
            CurrentPower.Value = definition.Power;
        }
        
        public void GetDamage(int damage)
        {
            CurrentPower.Value -= damage;

            if (CurrentPower.Value <= 0)
                CurrentPower.Value = 0;
        }
    }
}