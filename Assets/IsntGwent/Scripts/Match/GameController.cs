using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Runtime;

namespace IsntGwent.Scripts.Match
{
    public static class GameController
    {
        public static void StartGame(GameContext context)
        {
            Shuffle(context.Player1.Deck);
            Shuffle(context.Player2.Deck);
            
            DrawCards(context.Player1, 10);
            DrawCards(context.Player2, 10);

            var rnd = UnityEngine.Random.Range(0, 2);
            context.CurrentPlayer = rnd == 0 ? context.Player1 : context.Player2;
        }
        
        public static void Shuffle(List<CardInstance> deck)
        {
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);

                (deck[i], deck[j]) = (deck[j], deck[i]);
            }
        }

        public static void DrawCards(Player player, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                if (player.Hand.Count == player.MaxCardInHand)
                    return;
                
                var card = player.Deck.Last();
                player.Deck.Remove(card);
                
                player.Hand.Add(card);
                
            }
        }
    }
}