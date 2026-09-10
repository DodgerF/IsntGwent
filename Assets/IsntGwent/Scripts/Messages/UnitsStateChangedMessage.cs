using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct UnitsStateChangedMessage : NetworkMessage
    {
        public UnitStateChangedData[] Units;
    }
    
    public struct UnitStateChangedData
    {
        public string InstanceId;
        public int CurrentPower;
        public int BasePower;
        public int Armor;
        public bool IsDead;
    }
}