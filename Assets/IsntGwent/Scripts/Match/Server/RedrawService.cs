using System.Linq;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class RedrawService
    {
        public const int FirstRoundRedraws = 3;
        public const int RoundRedraws = 2;

        [Inject] private readonly MatchServerNotifier _notifier;

        public readonly Subject<GameContext> PhaseEnded = new();

        public void BeginPhase(GameContext context)
        {
            var amount = context.RoundNumber == 1 ? FirstRoundRedraws : RoundRedraws;

            Reset(context.Player1, amount);
            Reset(context.Player2, amount);

            context.IsRedrawPhase = true;

            _notifier.NotifyRedrawStarted(context, amount);
        }

        public void Redraw(GameContext context, Player player, string cardInstanceId)
        {
            if (!context.IsRedrawPhase) return;
            if (player.IsRedrawReady) return;
            if (player.RedrawsLeft <= 0) return;
            if (player.Deck.Count == 0) return;

            var card = player.Hand.FirstOrDefault(c => c.Id.ToString() == cardInstanceId);
            if (card == null) return;

            player.Hand.Remove(card);
            player.RedrawPile.Add(card);

            var drawn = player.Deck[0];
            player.Deck.RemoveAt(0);
            player.Hand.Add(drawn);

            player.RedrawsLeft--;

            _notifier.NotifyCardRedrawn(player, card, drawn, player.RedrawsLeft);

            if (player.RedrawsLeft == 0)
                SetReady(context, player);
        }

        public void SetReady(GameContext context, Player player)
        {
            if (!context.IsRedrawPhase) return;
            if (player.IsRedrawReady) return;

            player.IsRedrawReady = true;

            ReturnPileToDeck(player);

            if (!context.Player1.IsRedrawReady || !context.Player2.IsRedrawReady) return;

            context.IsRedrawPhase = false;

            _notifier.NotifyRedrawEnded(context);

            PhaseEnded.OnNext(context);
        }

        private static void Reset(Player player, int amount)
        {
            player.RedrawsLeft = amount;
            player.IsRedrawReady = false;
            player.RedrawPile.Clear();
        }

        private static void ReturnPileToDeck(Player player)
        {
            if (player.RedrawPile.Count == 0) return;

            player.Deck.AddRange(player.RedrawPile);
            player.RedrawPile.Clear();

            DeckService.Shuffle(player.Deck);
        }
    }
}
