using Mirror;
using IsntGwent.Scripts.Diagnostics;
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
                Log.Error(LogTag.Net, "No NetworkManager found");
                return;
            }

            if (AppRole.IsServer)
            {
                Log.Info(LogTag.Net, "starting server");
                NetworkManager.singleton.StartServer();
                return;
            }
            
            _connection.SetAutoReconnect(true);
            NetworkManager.singleton.StartClient();
        }
    }
}
