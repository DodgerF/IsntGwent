using Mirror;
using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Network
{
    public class MyNetManager : NetworkManager
    {
        public static Subject<NetworkConnectionToClient> ServerDisconnected = new();
        public static Subject<Unit> ClientConnected = new();
        public static Subject<Unit> ClientDisconnected = new();

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Debug.Log("OnServerDisconnect: " + conn.connectionId);
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
            ClientConnected.OnNext(Unit.Default);
        }

        public override void OnClientDisconnect()
        {
            Debug.Log("OnClientDisconnect");
            ClientDisconnected.OnNext(Unit.Default);
            base.OnClientDisconnect();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) return;
            if (NetworkServer.active) return;
            if (NetworkClient.active) return;

            ClientDisconnected.OnNext(Unit.Default);
        }

        public override void OnStartClient()
        {
            
        }
    }
}