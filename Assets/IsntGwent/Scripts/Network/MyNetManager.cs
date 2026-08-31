using Mirror;
using UniRx;
using IsntGwent.Scripts.Diagnostics;
using UnityEngine;

namespace IsntGwent.Scripts.Network
{
    public class MyNetManager : NetworkManager
    {
        private const string MenuScene = "Menu";

        public static Subject<NetworkConnectionToClient> ServerDisconnected = new();
        public static Subject<Unit> ClientConnected = new();
        public static Subject<Unit> ClientDisconnected = new();

        public override void Awake()
        {
            onlineScene = MenuScene;
            base.Awake();
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            Log.Info(LogTag.Net, "server: connection lost " + conn.connectionId);
            ServerDisconnected.OnNext(conn);
            base.OnServerDisconnect(conn);
        }
        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);
            Log.Info(LogTag.Net, "server: player added " + conn.connectionId);
        }
        public override void OnClientConnect()
        {
            ClientConnected.OnNext(Unit.Default);

            if (clientLoadedScene) return;

            BecomeReady();
        }

        public override void OnClientSceneChanged()
        {
            BecomeReady();
        }

        private static void BecomeReady()
        {
            if (!NetworkClient.connection.isAuthenticated) return;

            if (!NetworkClient.ready)
                NetworkClient.Ready();

            if (NetworkClient.localPlayer == null)
                NetworkClient.AddPlayer();
        }

        public override void OnClientDisconnect()
        {
            Log.Info(LogTag.Net, "client: disconnected");
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