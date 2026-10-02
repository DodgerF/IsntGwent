using IsntGwent.Scripts.Diagnostics;
using Mirror;
using Zenject;

namespace IsntGwent.Scripts.Network
{
    public class BootController : IInitializable
    {
        [Inject] private readonly ConnectionService _connection;

        public void Initialize()
        {
            if (NetworkManager.singleton == null)
            {
                Log.Error(LogTag.Net, "No NetworkManager found");
                return;
            }

            ServerEndpoint.Apply();

            if (AppRole.IsServer)
            {
                Log.Info(LogTag.Net, "starting server");
                NetworkManager.singleton.StartServer();
                return;
            }

            // Соединяемся всегда, даже без сессии itch: сцена меню приезжает от сервера,
            // и без соединения игрок не увидел бы вовсе ни меню, ни окна входа.
            // Без сессии он войдёт гостем, а меню сразу покажет вход и не пустит играть.
            _connection.SetAutoReconnect(true);
            NetworkManager.singleton.StartClient();
        }
    }
}
