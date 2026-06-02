using System.Collections.Generic;
using IsntGwent.Scripts.Decks.Definitions;

namespace IsntGwent.Scripts.Decks
{
    public class ClientDeckDatabase
    {
        public Dictionary<string, DeckDefinition> Decks;
        
        public void Setup(Dictionary<string, DeckDefinition> decks)
        {
            Decks = decks;
        }
    }
}