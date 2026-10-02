using System;
using System.Collections.Generic;

namespace IsntGwent.Scripts.Accounts.Server.Itch
{
    /// <summary>
    /// Две карты: незавершённые входы по nonce (state) и выданные сессии.
    /// ⚠️ Единственный класс аккаунтов, к которому ходят из двух потоков: страницу /auth/* обслуживает
    /// поток HttpListener, а проверку токена и запись аккаунта делает главный поток Unity.
    /// Поэтому всё под одним замком, и наружу отдаются только копии значений, а не сами записи.
    /// </summary>
    public class AuthSessionStore
    {
        /// Сколько живёт незавершённый вход: игрок открыл окно itch и не дошёл до конца.
        public static readonly TimeSpan PendingLifetime = TimeSpan.FromMinutes(10);

        /// Сколько живёт выданная сессия без единого использования.
        public static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(7);

        public enum Status
        {
            Unknown,
            Pending,
            Ready,
            Failed
        }

        public readonly struct PollResult
        {
            public readonly Status Status;
            public readonly string Session;
            public readonly string Nickname;
            public readonly string Error;

            public PollResult(Status status, string session, string nickname, string error)
            {
                Status = status;
                Session = session;
                Nickname = nickname;
                Error = error;
            }
        }

        private class Pending
        {
            public Status Status;
            public string Session;
            public string Nickname;
            public string Error;
            public DateTime Created;
        }

        private class Session
        {
            public string AccountId;
            public DateTime Touched;
        }

        private readonly object _lock = new();
        private readonly Dictionary<string, Pending> _byState = new();
        private readonly Dictionary<string, Session> _bySession = new();

        /// Заявка на вход. Возвращает false, если такой state уже обслуживается: повторный POST
        /// с тем же nonce — это либо перезагруженная вкладка, либо чужая попытка влезть в чужой вход.
        public bool TryBegin(string state)
        {
            if (string.IsNullOrEmpty(state)) return false;

            lock (_lock)
            {
                if (_byState.ContainsKey(state)) return false;

                _byState[state] = new Pending
                {
                    Status = Status.Pending,
                    Created = DateTime.UtcNow,
                };

                return true;
            }
        }

        public void Complete(string state, string session, string accountId, string nickname)
        {
            if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(session)) return;

            lock (_lock)
            {
                if (!_byState.TryGetValue(state, out var pending)) return;

                pending.Status = Status.Ready;
                pending.Session = session;
                pending.Nickname = nickname;

                _bySession[session] = new Session
                {
                    AccountId = accountId,
                    Touched = DateTime.UtcNow,
                };
            }
        }

        public void Fail(string state, string error)
        {
            if (string.IsNullOrEmpty(state)) return;

            lock (_lock)
            {
                if (!_byState.TryGetValue(state, out var pending)) return;

                pending.Status = Status.Failed;
                pending.Error = error;
            }
        }

        /// Опрос игрой. Готовый вход отдаётся ровно один раз: сессия уехала в игру — nonce больше не нужен.
        public PollResult Poll(string state)
        {
            if (string.IsNullOrEmpty(state)) return new PollResult(Status.Unknown, null, null, null);

            lock (_lock)
            {
                if (!_byState.TryGetValue(state, out var pending))
                    return new PollResult(Status.Unknown, null, null, null);

                if (pending.Status == Status.Pending)
                    return new PollResult(Status.Pending, null, null, null);

                _byState.Remove(state);

                return new PollResult(pending.Status, pending.Session, pending.Nickname, pending.Error);
            }
        }

        /// Кто стоит за токеном сессии. Пустая строка = сессии нет.
        public string ResolveAccount(string session)
        {
            if (string.IsNullOrEmpty(session)) return null;

            lock (_lock)
            {
                if (!_bySession.TryGetValue(session, out var entry)) return null;

                entry.Touched = DateTime.UtcNow;

                return entry.AccountId;
            }
        }

        public void Forget(string session)
        {
            if (string.IsNullOrEmpty(session)) return;

            lock (_lock) _bySession.Remove(session);
        }

        public void Cleanup()
        {
            var now = DateTime.UtcNow;

            lock (_lock)
            {
                RemoveWhere(_byState, entry => now - entry.Created > PendingLifetime);
                RemoveWhere(_bySession, entry => now - entry.Touched > SessionLifetime);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _byState.Clear();
                _bySession.Clear();
            }
        }

        private static void RemoveWhere<T>(Dictionary<string, T> map, Func<T, bool> isStale)
        {
            List<string> stale = null;

            foreach (var pair in map)
            {
                if (!isStale(pair.Value)) continue;

                stale ??= new List<string>();
                stale.Add(pair.Key);
            }

            if (stale == null) return;

            foreach (var key in stale) map.Remove(key);
        }
    }
}
