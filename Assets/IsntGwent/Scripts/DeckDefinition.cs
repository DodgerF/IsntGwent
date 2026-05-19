using System.Collections.Generic;

namespace IsntGwent.Scripts
{
    public class DeckDefinition
    {
        public string Id;
        public string Name;
        public string ImagePath;

        public List<DeckCardEntry> Cards = new();
    }

    public class DeckCardEntry
    {
        public string CardId;

        public int Count;
    }

}