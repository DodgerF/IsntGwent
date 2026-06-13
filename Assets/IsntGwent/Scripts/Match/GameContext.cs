using System.Collections.Generic;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Decks.Definitions;
using Mirror;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    public class Player
    {
        public readonly NetworkConnectionToClient Connection;
        public readonly List<CardInstance> Hand = new();
        public readonly int MaxCardInHand = 10;
        public readonly List<CardInstance> Deck;

        public Player(NetworkConnectionToClient connection, List<CardInstance> deck)
        {
            Connection = connection;
            Deck = deck;
        }
    }
    
    public class GameContext
    {
        [Inject] private readonly CardDatabase _cardDatabase;
        
        public Player Player1;
        public Player Player2;

        public Player GetPlayer(NetworkConnectionToClient connection)
        {
            return Player1.Connection == connection ? Player1 : Player2;
        }
        
        public Player CurrentPlayer;
        
        public void SetPlayers(PlayerLobby player1, PlayerLobby player2)
        {
            Player1 = new Player(player1.Connection, CreateDeck(player1.Deck));
            Player2 = new Player(player2.Connection, CreateDeck(player2.Deck));
        }

        private List<CardInstance> CreateDeck(DeckDefinition deckDefinition)
        {
            List<CardInstance> deck = new();

            foreach (var cardEnty in deckDefinition.Cards)
            {
                var cardDefinition = _cardDatabase.Get(cardEnty.CardId);
                
                for (int amount = 0; amount < cardEnty.Count; amount++)
                {
                    var card = CardFactory.Create(cardDefinition);
                    deck.Add(card);
                }
            }
            
            return deck;
        }
    }
}