using System;
using IsntGwent.Scripts.Cards.Definitions;
using UniRx;

namespace IsntGwent.Scripts.Cards.Runtime
{
    public class UnitInstance : CardInstance
    {
        public RowType RowType;

        public UnitDefinition UnitDefinition => (UnitDefinition)Definition;

        public readonly ReactiveProperty<int> CurrentPower = new();
        public readonly ReactiveProperty<int> Armor = new();

        public UnitInstance(UnitDefinition definition) : base(definition)
        {
            CurrentPower.Value = definition.Power;
        }

        public void GetDamage(int damage)
        {
            if (damage <= 0) return;

            if (Armor.Value > 0)
            {
                var absorbed = Math.Min(Armor.Value, damage);
                Armor.Value -= absorbed;
                damage -= absorbed;
            }

            if (damage <= 0) return;

            CurrentPower.Value -= damage;

            if (CurrentPower.Value <= 0)
                CurrentPower.Value = 0;
        }

        public void Kill()
        {
            Armor.Value = 0;
            CurrentPower.Value = 0;
        }

        public void ResetToBase()
        {
            CurrentPower.Value = UnitDefinition.Power;
            Armor.Value = 0;
        }
    }
}
