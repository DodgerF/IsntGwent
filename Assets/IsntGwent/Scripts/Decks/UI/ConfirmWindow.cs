using System;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Decks.UI
{
    public class ConfirmWindow : MonoBehaviour
    {
        public GameObject panel;
        public TextMeshProUGUI messageText;
        public Button confirmButton;
        public Button cancelButton;

        private Action _onConfirm;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void Start()
        {
            confirmButton.OnClickAsObservable()
                .Subscribe(_ => Confirm())
                .AddTo(this);

            cancelButton.OnClickAsObservable()
                .Subscribe(_ => Close())
                .AddTo(this);
        }

        public void Ask(string message, Action onConfirm)
        {
            _onConfirm = onConfirm;

            if (messageText != null) messageText.text = message;
            if (panel != null) panel.SetActive(true);
        }

        private void Confirm()
        {
            var action = _onConfirm;
            Close();
            action?.Invoke();
        }

        private void Close()
        {
            _onConfirm = null;
            if (panel != null) panel.SetActive(false);
        }
    }
}
