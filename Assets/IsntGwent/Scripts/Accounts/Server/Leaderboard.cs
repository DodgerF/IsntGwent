using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Accounts.Core;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Server
{
    public class Leaderboard
    {
        [Inject] private readonly IAccountStore _store;

        private readonly List<AccountData> _sorted = new();
        private bool _isDirty = true;

        public void Touch()
        {
            _isDirty = true;
        }

        public IReadOnlyList<AccountData> Top(int count)
        {
            Rebuild();

            return _sorted.Count <= count ? _sorted : _sorted.GetRange(0, count);
        }

        public int RankOf(string accountId)
        {
            if (string.IsNullOrEmpty(accountId)) return 0;

            Rebuild();

            for (var i = 0; i < _sorted.Count; i++)
            {
                if (_sorted[i].Id == accountId) return i + 1;
            }

            return 0;
        }

        private void Rebuild()
        {
            if (!_isDirty) return;

            _sorted.Clear();
            _sorted.AddRange(_store.All
                .OrderByDescending(a => a.Points)
                .ThenByDescending(a => a.Wins)
                .ThenBy(a => a.Id));

            _isDirty = false;
        }
    }
}
