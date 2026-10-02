using System;
using System.Collections;
using System.Collections.Generic;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Network
{
    /// <summary>
    /// Личность игрока устанавливается на этапе рукопожатия Mirror, а не отдельным сообщением после:
    /// соединение либо аутентифицировано и знает свой аккаунт, либо не поднимается вовсе.
    /// Отсюда <see cref="SeatRegistry"/> и лобби получают аккаунт даром, не таская его полем в каждом сообщении.
    ///
    /// ⚠️ Компонент вешает на себя <see cref="MyNetManager"/> в Awake, до того как соберутся сервисы сцены,
    /// поэтому Zenject сюда не достаёт: обе стороны цепляются статическими крючками.
    /// Серверный ставит AccountController, клиентский — PlayerAccount. Крючка нет — вход отбивается
    /// с <see cref="AccountError.NotReady"/>, и клиент повторит попытку сам.
    /// </summary>
    public class AccountAuthenticator : NetworkAuthenticator
    {
        /// Сервер: по токену сессии выдать ответ. Всю работу с хранилищем делает подписчик.
        public static Func<NetworkConnectionToClient, string, AuthResponseMessage> ServerAuthenticate;

        /// Клиент: чем представляемся. Пусто = гость.
        public static Func<string> ClientToken;

        /// Клиент: чем кончилось рукопожатие.
        public static readonly Subject<AuthResponseMessage> ClientResult = new();

        private readonly HashSet<NetworkConnectionToClient> _pendingDisconnect = new();

        public override void OnStartServer()
        {
            NetworkServer.RegisterHandler<AuthRequestMessage>(OnAuthRequest, false);
        }

        public override void OnStopServer()
        {
            NetworkServer.UnregisterHandler<AuthRequestMessage>();
        }

        public override void OnServerAuthenticate(NetworkConnectionToClient conn)
        {
            // Ждём AuthRequestMessage от клиента.
        }

        private void OnAuthRequest(NetworkConnectionToClient conn, AuthRequestMessage msg)
        {
            if (conn == null) return;
            if (_pendingDisconnect.Contains(conn)) return;

            var hook = ServerAuthenticate;

            var response = hook == null
                ? new AuthResponseMessage { IsSuccess = false, Error = AccountError.NotReady }
                : hook(conn, msg.SessionToken);

            conn.Send(response);

            if (response.IsSuccess)
            {
                ServerAccept(conn);
                return;
            }

            Log.Info(LogTag.Accounts, "auth rejected: " + response.Error);

            _pendingDisconnect.Add(conn);
            conn.isAuthenticated = false;

            // Секунда на доставку ответа: иначе игрок увидит обрыв вместо причины отказа.
            StartCoroutine(DelayedDisconnect(conn));
        }

        private IEnumerator DelayedDisconnect(NetworkConnectionToClient conn)
        {
            yield return new WaitForSeconds(1f);

            ServerReject(conn);

            yield return null;

            _pendingDisconnect.Remove(conn);
        }

        public override void OnStartClient()
        {
            NetworkClient.RegisterHandler<AuthResponseMessage>(OnAuthResponse, false);
        }

        public override void OnStopClient()
        {
            NetworkClient.UnregisterHandler<AuthResponseMessage>();
        }

        public override void OnClientAuthenticate()
        {
            var token = ClientToken == null ? string.Empty : ClientToken();

            NetworkClient.Send(new AuthRequestMessage { SessionToken = token ?? string.Empty });
        }

        private void OnAuthResponse(AuthResponseMessage msg)
        {
            ClientResult.OnNext(msg);

            if (msg.IsSuccess)
            {
                ClientAccept();
                return;
            }

            Log.Info(LogTag.Accounts, "server refused the session: " + msg.Error);

            ClientReject();
        }
    }
}
