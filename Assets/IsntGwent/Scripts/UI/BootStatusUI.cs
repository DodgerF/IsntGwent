using System;
using IsntGwent.Scripts.Network;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.UI
{
    public class BootStatusUI : MonoBehaviour
    {
        [Inject] private readonly ConnectionService _connection;

        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private float unavailableAfterSeconds = 10f;
        [SerializeField] private string connectingMessage = "Connecting…";
        [SerializeField] private string unavailableMessage = "Server unavailable";

        private readonly SerialDisposable _unavailableTimer = new();

        private void Start()
        {
            _unavailableTimer.AddTo(this);

            retryButton.OnClickAsObservable()
                .Subscribe(_ => OnRetryClicked())
                .AddTo(this);

            _connection.IsConnected
                .Subscribe(OnConnectionChanged)
                .AddTo(this);
        }

        private void OnConnectionChanged(bool isConnected)
        {
            panel.SetActive(!isConnected);

            if (isConnected)
                _unavailableTimer.Disposable = null;
            else
                ShowConnecting();
        }

        private void ShowConnecting()
        {
            statusText.text = connectingMessage;
            SetActionsVisible(false);

            _unavailableTimer.Disposable = Observable
                .Timer(TimeSpan.FromSeconds(unavailableAfterSeconds))
                .Subscribe(_ => ShowUnavailable());
        }

        private void ShowUnavailable()
        {
            statusText.text = unavailableMessage;
            SetActionsVisible(true);
        }

        private void SetActionsVisible(bool visible)
        {
            retryButton.gameObject.SetActive(visible);

            if (quitButton != null)
                quitButton.gameObject.SetActive(visible);
        }

        private void OnRetryClicked()
        {
            _connection.RetryNow();
            ShowConnecting();
        }
    }
}
