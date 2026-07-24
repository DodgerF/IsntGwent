using IsntGwent.Scripts.Decks.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Match.Client
{
    public class PlayerLobby
    {
        public readonly NetworkConnectionToClient Connection;
        public readonly DeckDefinition Deck;
        
        public PlayerLobby(NetworkConnectionToClient connection, DeckDefinition deck)
        {
            Connection = connection;
            Deck = deck;
        }
    }
}