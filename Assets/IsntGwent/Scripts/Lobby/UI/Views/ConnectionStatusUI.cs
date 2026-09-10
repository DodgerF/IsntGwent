using IsntGwent.Scripts.Network;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    public class ConnectionStatusUI : MonoBehaviour
    {
        [Inject] private readonly ConnectionService _connection;

        private void Start()
        {
            _connection.IsConnected
                .Subscribe(isConnected => gameObject.SetActive(!isConnected))
                .AddTo(this);
        }
    }
}
