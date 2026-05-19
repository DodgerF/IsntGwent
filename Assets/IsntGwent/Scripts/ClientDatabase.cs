using System.Collections.Generic;

namespace IsntGwent.Scripts
{
    public class ClientDatabase
    {
        public Dictionary<string, DeckDefinition> Decks;
        
        public void Setup(Dictionary<string, DeckDefinition> decks)
        {
            Decks = decks;
        }
    }
}