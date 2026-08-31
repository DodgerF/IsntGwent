using UniRx;

namespace IsntGwent.Scripts.Match.Server.Journal
{
    public class MatchJournalRecorder
    {
        public void Attach(GameContext context, MatchJournal journal)
        {
            if (context == null || journal == null) return;

            context.Journal = journal;

            context.AddDisposable(
                context.Events.Stream.Subscribe(gameEvent => Dispatch(context, gameEvent)));
        }

        private static void Dispatch(GameContext context, IGameEvent gameEvent)
        {
            var journal = context.Journal;
            if (journal == null) return;

            switch (gameEvent)
            {
                case TurnStarted started:
                    journal.TurnStart(started.Player);
                    break;

                case TurnEnded ended:
                    journal.TurnEnd(ended.Player);
                    break;

                case UnitDied died:
                    journal.Death(died.Owner, died.Unit, died.Row, died.SlotIndex, died.Killer);
                    break;

                case UnitSummoned summoned:
                    journal.Summon(context, summoned.Owner, summoned.Unit, summoned.FromDeck);
                    break;

                case UnitMoved moved:
                    journal.Move(moved.Owner, moved.Unit, moved.FromRow, moved.FromSlot, moved.ToRow, moved.ToSlot);
                    break;

                case UnitDevoured devoured:
                    journal.Devour(context, devoured.Owner, devoured.Consumer, devoured.Consumed);
                    break;
            }
        }
    }
}
