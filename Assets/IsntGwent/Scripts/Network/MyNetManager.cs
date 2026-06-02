using Mirror;
using UnityEngine;

namespace IsntGwent.Scripts.Network
{
    public class MyNetManager : NetworkManager
    {
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);
            Debug.Log("OnServerAddPlayer: " + conn.connectionId);
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            Debug.Log("OnServerDisconnect: " + conn.connectionId);
        }
        public override void OnClientConnect()
        {
            Debug.Log("Client connected");
            NetworkClient.Ready();
            NetworkClient.AddPlayer();
        }

        public override void OnStartClient()
        {
            Debug.Log("OnStartClient called");
        }
    }
}