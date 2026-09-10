using System.Collections.Generic;

namespace IsntGwent.Scripts.Match.Server.Bot
{
    public class BotRedrawRule
    {
        public string Card;
        public string With;
    }

    public class BotRedrawPlan
    {
        public List<string> DiscardDuplicates = new();
        public List<string> DiscardAlways = new();
        public List<BotRedrawRule> DiscardWith = new();
        public List<string> Seek = new();
    }

    public class BotWeights
    {
        public float EnemyDamage = 1f;
        public float AllyDamage = 1.3f;
        public float EnemyKill = 4f;
        public float KillValuePower = 4f;
        public float IdleDeathwishKill = 0.25f;
        public float AllyKill = 5f;
        public float BodyPower = 1f;
        public float WeatherOnEnemyMelee = 60f;
        public float WeatherOnEnemyRanged = 12f;
        public float WeatherOnOwnRow = -25f;
        public float VodyanoyPrep = 18f;
        public float PullUnderWeather = 14f;
        public float BondKill = 8f;
        public float DeathwishPenalty = 7f;
        public float HatchGain = 14f;
        public float BackRow = 4f;
        public float FrontRowPassive = 3f;
        public float Shield = 3f;
        public float LeshyPrep = 2.5f;
        public float BadExchange = 12f;
        public float SummonRoom = 5f;
        public float NoSummonRoom = 14f;
        public float TraitorNeighbor = 7f;
        public float TraitorDevourer = 12f;
        public float SweeperHold = 30f;
        public float WeatherRow = 9f;
        public float WastedAim = 14f;
        public float MoveIntoWeather = 10f;
        public float RowSweep = 3f;
        public float TraitorBait = 12f;
        public float TraitorAdjacency = 12f;
        public float NoGain = 25f;
        public float TraitorCombo = 26f;
        public float TraitorKill = 10f;
        public float TraitorSnipe = 16f;
    }

    public class BotProfile
    {
        public string Id = "starter_bot";
        public string DeckId = "started_deck";
        public string Nickname = "Соперник";

        public float SearchWaitMin = 10f;
        public float SearchWaitMax = 15f;

        public float TurnDelayMin = 2.2f;
        public float TurnDelayMax = 4f;
        public float AimDelay = 1.2f;
        public float ThinkChance = 0.25f;
        public float ThinkBonusMin = 1f;
        public float ThinkBonusMax = 2.5f;
        public float RedrawDelay = 0.9f;
        public float FinishDelay = 0.5f;

        public string WeatherCardId = "omut";
        public string WeatherCasterId = "vodyanoy";
        public string SweeperCardId = "leshy";

        public int SmallGap = 5;
        public int FirstRoundPassHand = 7;
        public int SweeperMinKills = 2;
        public int MaxCardsToChase = 2;
        public int EndgameHand = 2;
        public int SaveHand = 6;
        public int LeadToPass = 9;
        public float StrategicScore = 20f;

        public BotWeights Weights = new();
        public BotRedrawPlan Redraw = new();

        public BotProfile Copy() => (BotProfile)MemberwiseClone();
    }
}
