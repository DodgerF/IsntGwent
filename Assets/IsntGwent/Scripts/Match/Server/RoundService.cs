using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class RoundService
    {
        [Inject] private readonly MatchServerNotifier _notifier;
        [Inject] private readonly BoardSyncService _boardSync;
        [Inject] private readonly DeckService _deckService;
        [Inject] private readonly RedrawService _redrawService;

        public void EndRound(GameContext context)
        {
            var p1 = context.Player1;
            var p2 = context.Player2;

            var isTie = p1.TotalPower == p2.TotalPower;
            var winner = isTie ? null : p1.TotalPower > p2.TotalPower ? p1 : p2;
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

            var isGameEnded = p1.Hp <= 0 || p2.Hp <= 0;

            if (isGameEnded)
            {
                SendGameEnded(context, isTie, winner, loser);
                return;
            }

            BoardSyncService.MoveAllToGraveyard(context, p1);
            BoardSyncService.MoveAllToGraveyard(context, p2);
            _boardSync.Sync(context);
            _boardSync.SyncBoard(context);

            p1.IsPassed = false;
            p2.IsPassed = false;

            _deckService.DrawAndSync(context, p1, 1);
            _deckService.DrawAndSync(context, p2, 1);

            context.CurrentPlayer = isTie
                ? context.GetOpponent(context.CurrentPlayer)
                : winner;

            context.RoundNumber++;

            _redrawService.BeginPhase(context);
        }

        public void SendGameEnded(GameContext context, bool isTie, Player winner, Player loser)
        {
            _notifier.NotifyGameEnded(context, isTie, winner, loser);
            context.GameEnded.Value = true;
        }
    }
}
