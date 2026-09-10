namespace IsntGwent.Scripts.Cards.Definitions
{
    public enum LeadershipMode
    {
        Any,
        Held,
        Lost,
    }

    public class EffectCondition
    {
        public string AllyOnBoard;
        public bool AllyAbsent;
        public string Environment;
        public bool EnvironmentAbsent;
        public LeadershipMode Leadership;
        public int MinSourcePower;
    }
}
