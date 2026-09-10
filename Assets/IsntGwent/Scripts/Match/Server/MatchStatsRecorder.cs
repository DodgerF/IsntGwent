using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Accounts.Server;
using IsntGwent.Scripts.Diagnostics;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.Server
{
    public class MatchStatsRecorder
    {
        public const int WinPoints = 25;
        public const int LossPoints = 15;
        public const int TiePoints = 10;

        [Inject] private readonly IAccountStore _store;
        [Inject] private readonly Leaderboard _leaderboard;

        public void Record(GameContext context)
        {
            if (context == null) return;

            if (!context.IsRanked)
            {
                Log.Info(LogTag.Stats, "skipped: private room");
                return;
            }

            var winner = context.IsTie ? null : context.Winner;

            if (!context.IsTie && winner == null)
            {
                Log.Warn(LogTag.Stats, "skipped: no winner recorded");
                return;
            }

            var first = winner ?? context.Player1;
            var second = context.GetOpponent(first);

            var firstAccount = first?.Seat?.Account;
            var secondAccount = second?.Seat?.Account;

            if (firstAccount == null || secondAccount == null)
            {
                Log.Warn(LogTag.Stats, "skipped: seat without account");
                return;
            }

            if (firstAccount.Id == secondAccount.Id)
            {
                Log.Warn(LogTag.Stats, "skipped: same account on both seats");
                return;
            }

            var firstIsBot = first?.Seat is { IsBot: true };
            var secondIsBot = second?.Seat is { IsBot: true };

            if (firstIsBot && secondIsBot)
            {
                Log.Info(LogTag.Stats, "skipped: bots only");
                return;
            }

            if (context.IsTie)
            {
                if (!firstIsBot) ApplyTie(firstAccount);
                if (!secondIsBot) ApplyTie(secondAccount);
            }
            else
            {
                if (!firstIsBot) Apply(firstAccount, true);
                if (!secondIsBot) Apply(secondAccount, false);
            }

            if (!firstIsBot) _store.Save(firstAccount);
            if (!secondIsBot) _store.Save(secondAccount);

            _leaderboard.Touch();

            Log.Info(LogTag.Stats, $"{(context.IsTie ? "tie" : "win")}: " +
                                   $"{firstAccount.Nickname} {firstAccount.Points} : " +
                                   $"{secondAccount.Nickname} {secondAccount.Points}");
        }

        private static void Apply(AccountData account, bool isWin)
        {
            if (isWin)
            {
                account.Wins++;
                account.WinStreak++;
                account.LossStreak = 0;
            }
            else
            {
                account.Losses++;
                account.LossStreak++;
                account.WinStreak = 0;
            }

            account.Points = Mathf.Max(0, account.Points + PointsFor(account, isWin));
        }

        private static void ApplyTie(AccountData account)
        {
            account.Ties++;
            account.WinStreak = 0;
            account.LossStreak = 0;

            account.Points = Mathf.Max(0, account.Points + TiePoints);
        }

        private static int PointsFor(AccountData account, bool isWin)
        {
            return isWin ? WinPoints : -LossPoints;
        }
    }
}
