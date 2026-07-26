using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Runtime;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class DeckService
    {
        [Inject] private readonly MatchServerNotifier _notifier;

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
            if (player.Deck.Count == 0) return;

            for (int i = 0; i < amount; i++)
            {
                if (player.Deck.Count == 0) break;
                if (player.Hand.Count == player.MaxCardInHand) break;

                var card = player.Deck[0];
                player.Deck.RemoveAt(0);

                player.Hand.Add(card);
            }
        }

        public void DrawAndSync(GameContext context, Player player, int amount)
        {
            var before = player.Hand.Count;
            DrawCards(player, amount);
            var drawn = player.Hand.Count - before;
            if (drawn == 0) return;

            for (var i = player.Hand.Count - drawn; i < player.Hand.Count; i++)
            {
                _notifier.NotifyCardDrawn(player, player.Hand[i]);
            }

            _notifier.NotifyEnemyCardDrawn(context.GetOpponent(player), player.Hand.Count);
        }
    }
}
