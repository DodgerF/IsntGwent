using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public struct BotDeathwish
    {
        public int MyLoss;
        public int TheirLoss;
        public int MyGain;
        public int TheirGain;
    }

    public class BotOutcome
    {
        public int EnemyPowerLost;
        public int AllyPowerLost;
        public int SelfPowerGain;
        public int SummonedPower;

        public bool AppliesWeather;
        public bool NeedsAim;
        public bool MovesUnits;

        public int EnemiesAtOne;

        public readonly List<UnitInstance> EnemyVictims = new();
        public readonly List<UnitInstance> AllyCasualties = new();

        public int BodyPower;
        public int WeatherBite;

        public int WishMyLoss;
        public int WishTheirLoss;
        public int WishMyGain;
        public int WishTheirGain;

        public int PlannedGain;
        public int AimPool;

        public readonly Dictionary<UnitInstance, BotDeathwish> Wishes = new();

        public int EnemyKills => EnemyVictims.Count;
        public int AllyKills => AllyCasualties.Count;

        public int Swing => BodyPower + EnemyPowerLost - AllyPowerLost + SelfPowerGain + SummonedPower
                            + WishTheirLoss - WishMyLoss + WishMyGain - WishTheirGain + PlannedGain;
    }
}
