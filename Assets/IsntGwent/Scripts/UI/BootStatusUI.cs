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
        [SerializeField] private float unavailableAfterSeconds = 20f;
        [SerializeField] private int unavailableAfterAttempts = 3;
        [SerializeField] private string connectingMessage = "Connecting…";
        [SerializeField] private string unavailableMessage = "Server unavailable";

        private readonly SerialDisposable _unavailableTimer = new();

        private bool _isUnavailable;
        private int _failedAttempts;

        private void Awake()
        {
            panel.SetActive(true);
            statusText.text = connectingMessage;
            SetActionsVisible(false);
        }

        private void Start()
        {
            _unavailableTimer.AddTo(this);

            retryButton.OnClickAsObservable()
                .Subscribe(_ => OnRetryClicked())
                .AddTo(this);

            ShowConnecting();

            _connection.AttemptFailed
                .Subscribe(_ => OnAttemptFailed())
                .AddTo(this);
        }

        private void OnAttemptFailed()
        {
            if (_isUnavailable) return;

            _failedAttempts++;

            if (_failedAttempts >= unavailableAfterAttempts)
                ShowUnavailable();
        }

        private void ShowConnecting()
        {
            _isUnavailable = false;
            _failedAttempts = 0;

            panel.SetActive(true);
            statusText.text = connectingMessage;
            SetActionsVisible(false);

            _connection.SetAutoReconnect(true);

            _unavailableTimer.Disposable = Observable
                .Timer(TimeSpan.FromSeconds(unavailableAfterSeconds))
                .Subscribe(_ => ShowUnavailable());
        }

        private void ShowUnavailable()
        {
            _isUnavailable = true;
            _unavailableTimer.Disposable = null;

            _connection.SetAutoReconnect(false);

            panel.SetActive(true);
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
            ShowConnecting();
            _connection.RetryNow();
        }
    }
}
