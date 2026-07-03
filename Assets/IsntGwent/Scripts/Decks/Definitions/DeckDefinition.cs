
namespace IsntGwent.Scripts.Decks.Definitions
{
    public class DeckDefinition
    {
        public string Id;
        public string Name;
        public string ImagePath;

        public DeckCardEntry[] Cards;
    }

    public class DeckCardEntry
    {
        public string CardId;

        public int Count;
    }

}