using IsntGwent.Scripts.Decks.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct CreateLobbyMessage : NetworkMessage
    {
        public string Name;
        public string Password;
        public DeckDefinition Deck;
    }
}