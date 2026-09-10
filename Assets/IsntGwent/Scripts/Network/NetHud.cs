using Mirror;
using UnityEngine;

namespace IsntGwent.Scripts.Network
{
    public class NetHud : MonoBehaviour
    {
        public void OnClickServer()
        {
            if (NetworkClient.active)
                NetworkManager.singleton.StopClient();

            NetworkManager.singleton.StartServer();
        }
        
        public void OnClickClient()
        {
            NetworkManager.singleton.StartClient();
        }
    }
}
