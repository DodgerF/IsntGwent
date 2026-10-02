using System;
using System.Collections;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Network;
using Mirror;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Server.Itch
{
    /// <summary>
    /// Серверная половина входа через itch.io: принимает временный ключ игрока со страницы возврата,
    /// спрашивает у площадки, чей он, заводит аккаунт по числовому id и выдаёт свою сессию.
    ///
    /// Именно этот шаг делает подделку невозможной: игра не приносит серверу ни ника, ни id —
    /// она приносит ключ, который выдал itch, а кто за ним стоит, сервер спрашивает у itch сам.
    /// </summary>
    public class ItchAuthService : IInitializable, ITickable, IDisposable
    {
        private const float CleanupInterval = 60f;
        private const int RequestTimeoutSeconds = 15;

        [Inject] private readonly IAccountStore _store;
        [Inject] private readonly Leaderboard _leaderboard;
        [Inject] private readonly CoroutineRunner _runner;

        private readonly AuthSessionStore _sessions = new();

        private AuthHttpServer _http;
        private float _nextCleanup;

        public void Initialize()
        {
            if (!NetworkServer.active) return;

            _http = new AuthHttpServer(_sessions);
            _http.Start(ServerEndpoint.AuthHost, ServerEndpoint.AuthPort);
        }

        /// Кто стоит за токеном сессии. Зовётся из аутентификатора соединения, главный поток.
        public AccountData Resolve(string session)
        {
            var accountId = _sessions.ResolveAccount(session);
            if (string.IsNullOrEmpty(accountId)) return null;

            return _store.Find(accountId);
        }

        public void Tick()
        {
            if (_http == null) return;

            while (_http.TryDequeue(out var exchange))
                _runner.StartCoroutine(Verify(exchange));

            if (Time.unscaledTime < _nextCleanup) return;

            _nextCleanup = Time.unscaledTime + CleanupInterval;
            _sessions.Cleanup();
        }

        private IEnumerator Verify(AuthHttpServer.Exchange exchange)
        {
            using var request = UnityWebRequest.Get(ItchConfig.ProfileUrl);

            request.SetRequestHeader("Authorization", "Bearer " + exchange.Token);
            request.timeout = RequestTimeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Log.Warn(LogTag.Accounts, "auth: itch refused the token — " + request.error);
                _sessions.Fail(exchange.State, "itch rejected the token");
                yield break;
            }

            if (!TryReadProfile(request.downloadHandler.text, out var userId, out var nickname))
            {
                Log.Warn(LogTag.Accounts, "auth: cannot read itch profile answer");
                _sessions.Fail(exchange.State, "bad answer from itch");
                yield break;
            }

            var accountId = ItchConfig.AccountId(userId);
            var account = _store.GetOrCreate(accountId, nickname);

            if (account == null)
            {
                _sessions.Fail(exchange.State, "account storage failed");
                yield break;
            }

            // Ник на itch меняется, id — нет: вывеску подтягиваем на каждом входе.
            if (account.Nickname != nickname)
            {
                account.Nickname = nickname;
                _store.Save(account);
                _leaderboard.Touch();
            }

            _sessions.Complete(exchange.State, NewToken(), accountId, nickname);

            Log.Info(LogTag.Accounts, "auth: " + accountId + " signed in as " + nickname);
        }

        /// Ответ api.itch.io/profile: {"user":{"id":29789,"username":"...","display_name":"..."}}.
        private static bool TryReadProfile(string body, out long userId, out string nickname)
        {
            userId = 0;
            nickname = null;

            try
            {
                var user = JObject.Parse(body ?? string.Empty)["user"];
                if (user == null) return false;

                var id = user["id"];
                if (id == null) return false;

                userId = id.Value<long>();
                if (userId <= 0) return false;

                var display = (string)user["display_name"];
                var username = (string)user["username"];

                nickname = string.IsNullOrWhiteSpace(display) ? username : display;

                return !string.IsNullOrWhiteSpace(nickname);
            }
            catch (Exception e)
            {
                Log.Warn(LogTag.Accounts, "auth: broken profile json — " + e.Message);
                return false;
            }
        }

        private static string NewToken()
        {
            var bytes = new byte[32];

            using (var random = RandomNumberGenerator.Create())
                random.GetBytes(bytes);

            var builder = new StringBuilder(bytes.Length * 2);

            foreach (var b in bytes)
                builder.Append(b.ToString("x2", CultureInfo.InvariantCulture));

            return builder.ToString();
        }

        public void Dispose()
        {
            _http?.Dispose();
            _http = null;
            _sessions.Clear();
        }
    }
}
