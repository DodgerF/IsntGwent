using System;
using System.Globalization;
using IsntGwent.Scripts.Diagnostics;
using Mirror;
using Mirror.SimpleWeb;
using UnityEngine;
// У SimpleWeb свой Log в том же пространстве, что и транспорт — правило 6 проекта требует наш.
using Log = IsntGwent.Scripts.Diagnostics.Log;

namespace IsntGwent.Scripts.Network
{
    /// <summary>
    /// Куда стучится клиент и по какой схеме. Одно место на все платформы:
    /// поле networkAddress в сцене больше ничего не решает, его перебивает Apply перед StartClient.
    /// </summary>
    public static class ServerEndpoint
    {
        /// Домен за реверс-прокси: wss://WebHost:443 снаружи, ws://127.0.0.1:DirectPort внутри VPS.
        /// Сертификата на голый IP не выдают, поэтому у веба обязан быть домен, а не адрес.
        public const string WebHost = "dodgerf.com";
        public const ushort SecurePort = 443;

        /// Прямое подключение мимо прокси: редактор, десктопная сборка, локальный прогон WebGL по http.
        public const string DirectHost = "195.133.194.167";
        public const ushort DirectPort = 7182;

        /// HTTP-ручки входа через itch (/auth/*). Слушаем петлю: наружу их отдаёт Caddy по https,
        /// и открывать этот порт в мир не надо — там нет ни сертификата, ни проверки origin.
        public const string AuthHost = "127.0.0.1";
        public const ushort AuthPort = 7183;

        public static void Apply()
        {
            var manager = NetworkManager.singleton;
            if (manager == null) return;

            if (manager.transport is not SimpleWebTransport transport)
            {
                Log.Error(LogTag.Net, "expected SimpleWebTransport on NetworkManager, web build will not connect");
                return;
            }

            Resolve(out var host, out var port, out var secure);

            manager.networkAddress = host;
            transport.port = port;
            transport.clientUseWss = secure;
            transport.clientWebsocketSettings.ClientPortOption = WebsocketPortOption.DefaultSameAsServer;

            Log.Info(LogTag.Net, $"endpoint {(secure ? "wss" : "ws")}://{host}:{port}");
        }

        private static void Resolve(out string host, out ushort port, out bool secure)
        {
            host = DirectHost;
            port = DirectPort;
            secure = false;

            // Страница по https (так отдаёт itch) — браузер не пустит ws к серверу вовсе,
            // поэтому там и только там переходим на домен с сертификатом.
            if (IsSecurePage())
            {
                if (string.IsNullOrEmpty(WebHost))
                    Log.Error(LogTag.Net, "page is https but ServerEndpoint.WebHost is empty — set the domain");

                host = WebHost;
                port = SecurePort;
                secure = true;
            }

            var address = AppRole.Value("-address");
            if (!string.IsNullOrEmpty(address)) host = address;

            if (ushort.TryParse(AppRole.Value("-port"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var custom))
                port = custom;
        }

        private static bool IsSecurePage()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer) return false;

            return Application.absoluteURL.StartsWith("https", StringComparison.OrdinalIgnoreCase);
        }
    }
}
