using Mirror;
using UnityEngine;
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
                Debug.LogError("No NetworkManager found");
                return;
            }

            if (IsServerLaunch())
            {
                NetworkManager.singleton.StartServer();
                NetworkManager.singleton.ServerChangeScene("Menu");
                return;
            }
            
            _connection.SetAutoReconnect(true);
            NetworkManager.singleton.StartClient();
        }

        private static bool IsServerLaunch()
        {
            return Application.isBatchMode ||
                   System.Array.Exists(System.Environment.GetCommandLineArgs(), arg => arg == "-server");
        }
    }
}
