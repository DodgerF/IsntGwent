using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;

namespace IsntGwent.Scripts.Content
{
    public class CardWire
    {
        public string Id;
        public string Name;
        public string ImageName;
        public CardType Type;
        public CardRarity Rarity;
        public int Power;
        public string Description;
        public int MaxCopies;
        public bool IsToken;

        public bool RequiresTargets;
        public int TargetCount;
        public bool AllowPartial;
        public bool TargetIncludeAllies;
        public bool TargetIncludeEnemies;
        public bool AimsAfterPlace;
        public bool RequiresRow;
    }

    public class CardsWire
    {
        public int ContentVersion;
        public List<CardWire> Cards = new();
    }

    public class RulesWire
    {
        public int MinDeckSize;
        public int MaxCopiesCommon;
        public int MaxCopiesRare;
        public int MaxCopiesEpic;
        public int MaxCopiesLegendary;
    }

    public class ManifestFileWire
    {
        public string Path;
        public string Sha1;
        public int Size;
    }

    public class ManifestAtlasWire
    {
        public string Path;
        public string Sha1;
        public int Cell;
    }

    public class ManifestWire
    {
        public int ContentVersion;
        public RulesWire Rules;
        public List<ManifestFileWire> Files = new();
        public ManifestAtlasWire Atlas;
        public int PollTimeoutMs;
    }
}
