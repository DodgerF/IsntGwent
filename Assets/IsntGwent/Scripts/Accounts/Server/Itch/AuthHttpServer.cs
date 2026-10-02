using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Threading;
using IsntGwent.Scripts.Diagnostics;
using Newtonsoft.Json.Linq;

namespace IsntGwent.Scripts.Accounts.Server.Itch
{
    /// <summary>
    /// Маленький HTTP-слушатель сервера игры: две ручки для входа через itch.
    /// Наружу он не смотрит — снаружи стоит Caddy с сертификатом и проксирует /auth/* сюда,
    /// поэтому слушаем петлю, а не все интерфейсы.
    ///
    ///   POST /auth/exchange  {"state": "...", "access_token": "..."}  — от страницы /oauth/callback
    ///   GET  /auth/poll?state=...                                     — от самой игры
    ///
    /// ⚠️ Оба обработчика крутятся в потоке пула, а не в главном потоке Unity: трогать отсюда
    /// можно только <see cref="AuthSessionStore"/> и очередь, ничего из Unity API.
    /// </summary>
    public class AuthHttpServer : IDisposable
    {
        public readonly struct Exchange
        {
            public readonly string State;
            public readonly string Token;

            public Exchange(string state, string token)
            {
                State = state;
                Token = token;
            }
        }

        private const int MaxBodyBytes = 4096;
        private const int MaxFieldLength = 512;

        private readonly AuthSessionStore _sessions;
        private readonly ConcurrentQueue<Exchange> _incoming = new();

        private HttpListener _listener;
        private int _stopping;

        public AuthHttpServer(AuthSessionStore sessions)
        {
            _sessions = sessions;
        }

        public bool TryDequeue(out Exchange exchange) => _incoming.TryDequeue(out exchange);

        public void Start(string host, ushort port)
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://" + host + ":" + port + "/");
                _listener.Start();
            }
            catch (Exception e)
            {
                Log.Error(LogTag.Accounts, "auth: cannot listen on " + host + ":" + port + " — " + e.Message);
                _listener = null;
                return;
            }

            Log.Info(LogTag.Accounts, "auth: listening on http://" + host + ":" + port + "/");

            Listen();
        }

        private void Listen()
        {
            if (_listener == null) return;

            try
            {
                _listener.BeginGetContext(OnContext, null);
            }
            catch (Exception e)
            {
                if (Volatile.Read(ref _stopping) == 0)
                    Log.Error(LogTag.Accounts, "auth: listener broken — " + e.Message);
            }
        }

        private void OnContext(IAsyncResult result)
        {
            if (_listener == null) return;
            if (Volatile.Read(ref _stopping) != 0) return;

            HttpListenerContext context;

            try
            {
                context = _listener.EndGetContext(result);
            }
            catch (Exception)
            {
                // Слушатель закрыли или соединение оборвалось — следующий круг разберётся.
                Listen();
                return;
            }

            Listen();

            try
            {
                Handle(context);
            }
            catch (Exception e)
            {
                Log.Error(LogTag.Accounts, "auth: request failed — " + e.Message);

                try
                {
                    Respond(context, 500, "{\"status\":\"error\"}");
                }
                catch (Exception)
                {
                    // Клиент уже отвалился — писать некуда.
                }
            }
        }

        private void Handle(HttpListenerContext context)
        {
            var request = context.Request;
            var path = request.Url == null ? string.Empty : request.Url.AbsolutePath;

            if (request.HttpMethod == "OPTIONS")
            {
                Respond(context, 204, string.Empty);
                return;
            }

            if (path == "/auth/poll" && request.HttpMethod == "GET")
            {
                Poll(context, request.QueryString["state"]);
                return;
            }

            if (path == "/auth/exchange" && request.HttpMethod == "POST")
            {
                HandleExchange(context, ReadBody(request));
                return;
            }

            if (path == "/auth/health")
            {
                Respond(context, 200, "{\"status\":\"ok\"}");
                return;
            }

            Respond(context, 404, "{\"status\":\"error\",\"error\":\"unknown endpoint\"}");
        }

        private void Poll(HttpListenerContext context, string state)
        {
            if (!IsSane(state))
            {
                Respond(context, 400, "{\"status\":\"error\",\"error\":\"bad state\"}");
                return;
            }

            var result = _sessions.Poll(state);

            switch (result.Status)
            {
                case AuthSessionStore.Status.Ready:
                    Respond(context, 200, "{\"status\":\"ready\",\"session\":" + Quote(result.Session) +
                                          ",\"nickname\":" + Quote(result.Nickname) + "}");
                    return;

                case AuthSessionStore.Status.Failed:
                    Respond(context, 200, "{\"status\":\"error\",\"error\":" + Quote(result.Error) + "}");
                    return;

                default:
                    // Пока страница возврата не дошла до сервера, состояния нет вовсе —
                    // для игры это то же самое ожидание, что и Pending.
                    Respond(context, 200, "{\"status\":\"pending\"}");
                    return;
            }
        }

        private void HandleExchange(HttpListenerContext context, string body)
        {
            string state = null;
            string token = null;

            try
            {
                var json = JObject.Parse(body ?? string.Empty);
                state = (string)json["state"];
                token = (string)json["access_token"];
            }
            catch (Exception)
            {
                // Разберёт проверка ниже: пустые поля — плохой запрос.
            }

            if (!IsSane(state) || !IsSane(token))
            {
                Respond(context, 400, "{\"status\":\"error\",\"error\":\"bad request\"}");
                return;
            }

            if (!_sessions.TryBegin(state))
            {
                Respond(context, 409, "{\"status\":\"error\",\"error\":\"state already used\"}");
                return;
            }

            _incoming.Enqueue(new Exchange(state, token));

            Respond(context, 202, "{\"status\":\"accepted\"}");
        }

        private static string ReadBody(HttpListenerRequest request)
        {
            using var stream = request.InputStream;
            var buffer = new byte[MaxBodyBytes];
            var read = 0;

            while (read < buffer.Length)
            {
                var chunk = stream.Read(buffer, read, buffer.Length - read);
                if (chunk <= 0) break;

                read += chunk;
            }

            return Encoding.UTF8.GetString(buffer, 0, read);
        }

        private static void Respond(HttpListenerContext context, int code, string body)
        {
            var response = context.Response;
            var bytes = Encoding.UTF8.GetBytes(body ?? string.Empty);

            response.StatusCode = code;
            response.KeepAlive = false;
            response.ContentType = "application/json; charset=utf-8";
            response.ContentLength64 = bytes.Length;

            // Игра на itch крутится в iframe со случайного адреса CDN, то есть опрашивает нас
            // из чужого origin. По этой ручке отдаётся только то, что знает владелец nonce.
            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            response.Headers["Cache-Control"] = "no-store";

            using var output = response.OutputStream;
            output.Write(bytes, 0, bytes.Length);
        }

        private static bool IsSane(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (value.Length > MaxFieldLength) return false;

            foreach (var c in value)
            {
                if (char.IsControl(c)) return false;
            }

            return true;
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";

            var builder = new StringBuilder(value.Length + 2);
            builder.Append('"');

            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < ' ') builder.Append("\\u").Append(((int)c).ToString("x4"));
                        else builder.Append(c);
                        break;
                }
            }

            builder.Append('"');

            return builder.ToString();
        }

        public void Dispose()
        {
            Interlocked.Exchange(ref _stopping, 1);

            if (_listener == null) return;

            try
            {
                _listener.Stop();
                _listener.Close();
            }
            catch (Exception)
            {
                // Закрываемся — жаловаться уже некому.
            }

            _listener = null;
        }
    }
}
