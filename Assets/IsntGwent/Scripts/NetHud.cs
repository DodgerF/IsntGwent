using Mirror;
using UnityEngine;

namespace IsntGwent.Scripts
{
    public class NetHud : MonoBehaviour
    {
        public void OnClickServer()
        {
            NetworkManager.singleton.StartServer();
            NetworkManager.singleton.ServerChangeScene("Menu");
        }
        
        public void OnClickClient()
        {
            NetworkManager.singleton.StartClient();
        }
    }
}
