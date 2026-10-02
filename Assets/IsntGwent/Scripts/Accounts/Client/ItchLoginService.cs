using System;
using System.Collections;
using System.Runtime.InteropServices;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Diagnostics;
using Mirror;
using Newtonsoft.Json.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace IsntGwent.Scripts.Accounts.Client
{
    public enum ItchLoginState
    {
        /// Ничего не просили.
        Idle,

        /// Окно разрешения открыто, ждём, пока игрок нажмёт «разрешить».
        Waiting,

        /// Сессия получена.
        Done,

        /// Не вышло: закрыли окно, отказали, сервер не ответил.
        Failed
    }

    /// <summary>
    /// Клиентская половина входа через itch.io. Открывает страницу разрешения по нажатию игрока
    /// и опрашивает наш сервер, пока тот не отдаст сессию.
    ///
    /// Почему опрос, а не postMessage: игра на itch крутится в песочном iframe со случайного адреса
    /// CDN, строго проверить отправителя нечем, а светить токен площадки в postMessage(…, '*') нельзя.
    /// </summary>
    public class ItchLoginService : IInitializable, IDisposable
    {
        private const string SessionKey = "account.session";

        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan PollTimeout = TimeSpan.FromMinutes(5);

        [Inject] private readonly CoroutineRunner _runner;

        public readonly ReactiveProperty<ItchLoginState> State = new(ItchLoginState.Idle);
        public readonly ReactiveProperty<string> Error = new(string.Empty);
        public readonly Subject<string> OnSession = new();

        /// Всплывающее окно зарезали: мост откроет его по следующему клику игрока.
        public readonly ReactiveProperty<bool> NeedsSecondClick = new(false);

        private Coroutine _poll;

        /// Токен нашей сессии, а не токен itch: itch-ключ живёт только на сервере и сюда не попадает.
        public readonly ReactiveProperty<string> SessionToken = new(string.Empty);

        public string Session => SessionToken.Value;

        public bool HasSession => !string.IsNullOrEmpty(Session);

        /// В браузере гостя нет: там площадка своя и вход обязателен. В редакторе и на десктопе
        /// itch взять неоткуда, поэтому соединение поднимается гостевым.
        public static bool IsRequired => Application.platform == RuntimePlatform.WebGLPlayer;

        /// Адрес страницы разрешения — на случай, когда всплывающее окно зарезали и его надо открыть руками.
        public string AuthUrl { get; private set; } = string.Empty;

        public void Initialize()
        {
            if (NetworkServer.active) return;

            SessionToken.Value = PlayerPrefs.GetString(SessionKey, string.Empty);
        }

        public void Begin()
        {
            if (State.Value == ItchLoginState.Waiting) return;

            var state = Guid.NewGuid().ToString("N");

            AuthUrl = ItchConfig.BuildAuthUrl(state);
            Error.Value = string.Empty;
            State.Value = ItchLoginState.Waiting;

            var opened = OpenAuthPage(AuthUrl);

            if (opened == 0)
            {
                Fail("popup blocked");
                return;
            }

            // 2 — браузер зарезал окно, но мост открыл его по следующему клику: игроку надо сказать.
            NeedsSecondClick.Value = opened == 2;

            Stop();
            _poll = _runner.StartCoroutine(Poll(state));
        }

        /// Сервер не знает нашей сессии (перезапустился, или она истекла) — забываем и просим войти заново.
        public void Forget()
        {
            SessionToken.Value = string.Empty;

            PlayerPrefs.DeleteKey(SessionKey);
            PlayerPrefs.Save();

            State.Value = ItchLoginState.Idle;
        }

        private IEnumerator Poll(string state)
        {
            var url = ItchConfig.BuildPollUrl(state);
            var deadline = Time.unscaledTime + (float)PollTimeout.TotalSeconds;

            while (Time.unscaledTime < deadline)
            {
                // Новый экземпляр на каждый круг: у WaitForSecondsRealtime внутри своё состояние,
                // и переиспользованный перестаёт ждать вовсе.
                yield return new WaitForSecondsRealtime((float)PollInterval.TotalSeconds);

                using var request = UnityWebRequest.Get(url);

                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // Сервер мог перезапускаться или прокси моргнуть — это не отказ, ждём дальше.
                    Log.Info(LogTag.Accounts, "login: poll failed — " + request.error);
                    continue;
                }

                if (!TryRead(request.downloadHandler.text, out var status, out var session, out var error))
                    continue;

                if (status == "pending") continue;

                if (status == "ready")
                {
                    Accept(session);
                    yield break;
                }

                Fail(string.IsNullOrEmpty(error) ? "itch refused" : error);
                yield break;
            }

            Fail("timed out");
        }

        private void Accept(string session)
        {
            if (string.IsNullOrEmpty(session))
            {
                Fail("empty session");
                return;
            }

            SessionToken.Value = session;

            PlayerPrefs.SetString(SessionKey, session);
            PlayerPrefs.Save();

            State.Value = ItchLoginState.Done;
            Error.Value = string.Empty;
            NeedsSecondClick.Value = false;

            OnSession.OnNext(session);
        }

        private void Fail(string reason)
        {
            Log.Info(LogTag.Accounts, "login failed: " + reason);

            Error.Value = reason;
            NeedsSecondClick.Value = false;
            State.Value = ItchLoginState.Failed;
        }

        private static bool TryRead(string body, out string status, out string session, out string error)
        {
            status = null;
            session = null;
            error = null;

            try
            {
                var json = JObject.Parse(body ?? string.Empty);

                status = (string)json["status"];
                session = (string)json["session"];
                error = (string)json["error"];

                return !string.IsNullOrEmpty(status);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void Stop()
        {
            if (_poll == null) return;

            _runner.StopCoroutine(_poll);
            _poll = null;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int ItchOpenLogin(string url);

        /// 1 — окно открыто, 2 — откроется по следующему клику, 0 — не вышло вовсе.
        private static int OpenAuthPage(string url)
        {
            return ItchOpenLogin(url);
        }
#else
        private static int OpenAuthPage(string url)
        {
            // Вне браузера входа через itch не бывает; путь оставлен для ручной проверки из редактора.
            Application.OpenURL(url);
            return 1;
        }
#endif

        public void Dispose()
        {
            Stop();
        }
    }
}
