using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace IsntGwent.Scripts.Network
{
    public class NetDebugHud : MonoBehaviour
    {
        private const float Width = 230f;
        private const float Margin = 10f;

        private readonly List<NetworkConnectionToClient> _buffer = new();

        private bool _expanded;

        private void OnGUI()
        {
            var area = new Rect(
                Screen.width - Width - Margin,
                Margin,
                Width,
                Screen.height - Margin * 2f);

            GUILayout.BeginArea(area);

            if (GUILayout.Button(_expanded ? "NET DEBUG ▲" : "NET DEBUG ▼"))
                _expanded = !_expanded;

            if (_expanded)
                DrawBody();

            GUILayout.EndArea();
        }

        private void DrawBody()
        {
            if (NetworkServer.active)
            {
                DrawServer();
                return;
            }

            if (NetworkClient.active)
            {
                DrawClient();
                return;
            }

            GUILayout.Label("Neither server nor client");
        }

        private void DrawServer()
        {
            GUILayout.Label("Server. Connections: " + NetworkServer.connections.Count);

            _buffer.Clear();
            _buffer.AddRange(NetworkServer.connections.Values.Where(c => c != null));

            foreach (var conn in _buffer)
            {
                if (!GUILayout.Button("Drop conn #" + conn.connectionId)) continue;

                Debug.Log("NetDebugHud: разрываем conn " + conn.connectionId);
                conn.Disconnect();
            }
        }

        private void DrawClient()
        {
            GUILayout.Label(NetworkClient.isConnected ? "Client: connected" : "Client: connecting…");

            if (!GUILayout.Button("Drop own connection")) return;

            Debug.Log("NetDebugHud: рвём своё соединение");
            NetworkClient.Disconnect();
        }
    }
}
