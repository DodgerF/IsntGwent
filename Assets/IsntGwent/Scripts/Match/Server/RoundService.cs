using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class RoundService
    {
        public const int RoundDraw = 3;

        [Inject] private readonly MatchServerNotifier _notifier;
        [Inject] private readonly BoardSyncService _boardSync;
        [Inject] private readonly DeckService _deckService;
        [Inject] private readonly RedrawService _redrawService;

        public void EndRound(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            var power1 = p1.TotalPower;
            var power2 = p2.TotalPower;

            var isTie = power1 == power2;
            var winner = isTie ? null : power1 > power2 ? p1 : p2;
            var loser = winner != null ? context.GetOpponent(winner) : null;

            context.Publish(new RoundEnded(winner, isTie));
            _boardSync.Sync(context);

            _notifier.NotifyRoundResult(context, isTie, winner);

            if (isTie)
            {
                p1.Hp--;
                p2.Hp--;
            }
            else
            {
                loser.Hp--;
            }

            _notifier.NotifyHpChanged(context);

            context.Journal?.RoundEnd(context, winner, isTie, power1, power2);

            var isGameEnded = p1.Hp <= 0 || p2.Hp <= 0;

            if (isGameEnded)
            {
                SendGameEnded(context);
                return;
            }

            var pendingDropped = DiscardPendingPlays(p1) | DiscardPendingPlays(p2);

            BoardSyncService.MoveAllToGraveyard(context, p1);
            BoardSyncService.MoveAllToGraveyard(context, p2);
            ClearWeather(context, p1);
            ClearWeather(context, p2);
            _boardSync.Sync(context);
            _boardSync.SyncBoard(context);

            if (pendingDropped)
                _notifier.NotifyPendingPlay(context, p1);

            p1.IsPassed = false;
            p2.IsPassed = false;

            _deckService.DrawAndSync(context, p1, RoundDraw);
            _deckService.DrawAndSync(context, p2, RoundDraw);

            context.CurrentPlayer = isTie
                ? context.GetOpponent(context.CurrentPlayer)
                : winner;

            context.RoundNumber++;

            _redrawService.BeginPhase(context);
        }

        private static void ClearWeather(GameContext context, Player player)
        {
            if (player.Weather.Count > 0)
                context.Journal?.WeatherCleared(player);

            WeatherService.Clear(player);
        }

        private static bool DiscardPendingPlays(Player player)
        {
            if (player.PendingPlays.Count == 0) return false;

            player.Graveyard.AddRange(player.PendingPlays);
            player.PendingPlays.Clear();

            return true;
        }

        public void SendGameEnded(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            var isTie = p1.Hp <= 0 && p2.Hp <= 0;
            var winner = isTie ? null : p1.Hp > p2.Hp ? p1 : p2;
            var loser = winner != null ? context.GetOpponent(winner) : null;

            _notifier.NotifyGameEnded(context, isTie, winner, loser);

            context.EndReason = "hp";
            context.IsTie = isTie;
            context.Winner = winner;
            context.GameEnded.Value = true;
        }
    }
}
