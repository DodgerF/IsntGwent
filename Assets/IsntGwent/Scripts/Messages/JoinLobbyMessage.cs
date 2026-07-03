using IsntGwent.Scripts.Decks.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct JoinLobbyMessage : NetworkMessage
    {
        public string LobbyId;
        public string Password;
        public DeckDefinition Deck;
    }
}