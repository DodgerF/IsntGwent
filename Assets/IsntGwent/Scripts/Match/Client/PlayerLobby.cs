using IsntGwent.Scripts.Decks.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Match.Client
{
    public class PlayerLobby
    {
        public NetworkConnectionToClient Connection;
        public readonly DeckDefinition Deck;
        public bool IsReady;

        public PlayerLobby(NetworkConnectionToClient connection, DeckDefinition deck)
        {
            Connection = connection;
            Deck = deck;
        }
    }
}
