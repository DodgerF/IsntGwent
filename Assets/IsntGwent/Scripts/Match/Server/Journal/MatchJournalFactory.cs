using System;
using System.IO;
using IsntGwent.Scripts.Diagnostics;
using Mirror;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Journal
{
    public class MatchJournalFactory
    {
        [Inject] private readonly LogService _logs;
        [Inject] private readonly MatchJournalRecorder _recorder;

        public MatchJournal Create(GameContext context, string lobbyId)
        {
            if (context == null) return null;

            context.MatchId = NewId();
            context.StartedAt = DateTime.Now;

            if (!NetworkServer.active) return null;

            var directory = _logs.IsJournalEnabled
                ? Path.Combine(_logs.MatchesDirectory, DateTime.Now.ToString("yyyy-MM-dd"))
                : null;

            var journal = MatchJournal.Open(context.MatchId, directory);

            _recorder.Attach(context, journal);
            journal.Start(context, lobbyId);

            return journal;
        }

        private static string NewId()
        {
            return DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        }
    }
}
