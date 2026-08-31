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

            var p1 = context.Player1;
            var p2 = context.Player2;

            Reset(p1, p1.Deck.Count == 0 ? 0 : amount);
            Reset(p2, p2.Deck.Count == 0 ? 0 : amount);

            context.IsRedrawPhase = true;

            _notifier.NotifyRedrawStarted(context, p1, p1.RedrawsLeft);
            _notifier.NotifyRedrawStarted(context, p2, p2.RedrawsLeft);

            context.Journal?.RedrawStart(context);

            if (p1.RedrawsLeft == 0) SetReady(context, p1);
            if (p2.RedrawsLeft == 0) SetReady(context, p2);
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

            if (player.Deck.Count == 0) player.RedrawsLeft = 0;

            context.Journal?.Redraw(player, card, drawn);

            _notifier.NotifyCardRedrawn(player, card, drawn, player.RedrawsLeft);
            _notifier.NotifyDecks(context);

            if (player.RedrawsLeft == 0)
                SetReady(context, player);
        }

        public void SetReady(GameContext context, Player player)
        {
            if (!context.IsRedrawPhase) return;
            if (player.IsRedrawReady) return;

            player.IsRedrawReady = true;
            context.Journal?.RedrawReady(player);

            ReturnPileToDeck(player);

            _notifier.NotifyDecks(context);

            if (!context.Player1.IsRedrawReady || !context.Player2.IsRedrawReady) return;

            context.IsRedrawPhase = false;
            context.Journal?.RedrawEnd();

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
