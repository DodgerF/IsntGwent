using Mirror;
using UnityEngine;

namespace IsntGwent.Scripts.Network
{
    public class AutoServer : MonoBehaviour
    {
        private void Start()
        {
            if (NetworkManager.singleton == null)
            {
                Debug.LogError("No NetworkManager found");
                return;
            }

            if (Application.isBatchMode ||
                System.Array.Exists(System.Environment.GetCommandLineArgs(), arg => arg == "-server"))
            {
                NetworkManager.singleton.StartServer();
                NetworkManager.singleton.ServerChangeScene("Menu");
            }
            else
            {
                NetworkManager.singleton.StartClient();
            }
        }
    }
}
