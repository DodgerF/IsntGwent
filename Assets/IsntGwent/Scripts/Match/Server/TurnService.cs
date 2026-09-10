using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class TurnService
    {
        [Inject] private readonly MatchServerNotifier _notifier;
        [Inject] private readonly RoundService _roundService;
        [Inject] private readonly BoardSyncService _boardSync;

        public void PassTurn(GameContext context, Player player)
        {
            player.IsPassed = true;
            context.Journal?.Pass(player);

            var opponent = context.GetOpponent(player);

            context.Publish(new TurnEnded(player));
            _notifier.NotifyTurnChanged(player, false);

            if (opponent.IsPassed)
            {
                _roundService.EndRound(context);
                return;
            }

            _notifier.NotifyEnemyPassed(opponent);

            context.CurrentPlayer = opponent;
            _notifier.NotifyTurnChanged(opponent, true);

            StartTurn(context, opponent);
        }

        public void ChangeTurn(GameContext context)
        {
            var currentPlayer = context.CurrentPlayer;
            var opponent = context.GetOpponent(currentPlayer);

            context.Publish(new TurnEnded(currentPlayer));

            if (opponent.IsPassed)
            {
                RunSkippedTurn(context, opponent);

                _notifier.NotifyTurnChanged(currentPlayer, true);
                StartTurn(context, currentPlayer);
                return;
            }

            context.CurrentPlayer = opponent;
            _notifier.NotifyTurnChanged(currentPlayer, false);
            _notifier.NotifyTurnChanged(opponent, true);

            StartTurn(context, opponent);
        }

        private void RunSkippedTurn(GameContext context, Player player)
        {
            context.Publish(new TurnStarted(player));
            context.Publish(new TurnEnded(player));
            _boardSync.Sync(context);

            if (context.FlushBoardDirty())
                _boardSync.SyncBoard(context);
        }

        private void StartTurn(GameContext context, Player player)
        {
            context.Publish(new TurnStarted(player));
            _boardSync.Sync(context);
        }
    }
}
