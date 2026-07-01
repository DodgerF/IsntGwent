using Mirror;
using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Network
{
    public class MyNetManager : NetworkManager
    {
        public static Subject<NetworkConnectionToClient> ServerDisconnected = new();
        
        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            ServerDisconnected.OnNext(conn);
            base.OnServerDisconnect(conn);
        }
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);
            Debug.Log("OnServerAddPlayer: " + conn.connectionId);
        }
        public override void OnClientConnect()
        {
            NetworkClient.Ready();
            NetworkClient.AddPlayer();
        }

        public override void OnStartClient()
        {
            
        }
    }
}