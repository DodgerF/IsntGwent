using System;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Lobby.Core;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    public class LobbyErrorUI : MonoBehaviour
    {
        private static readonly TimeSpan ShowDuration = TimeSpan.FromSeconds(1);

        [Inject] private readonly LobbyClientHandler _handler;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private GameObject panel;

        private readonly SerialDisposable _hideTimer = new();

        private void Awake()
        {
            panel.SetActive(false);
        }

        private void Start()
        {
            _hideTimer.AddTo(this);

            _handler.OnError
                .Subscribe(Show)
                .AddTo(this);
        }

        private void Show(LobbyError error)
        {
            errorText.text = Describe(error);
            panel.SetActive(true);

            _hideTimer.Disposable = Observable
                .Timer(ShowDuration)
                .Subscribe(_ => panel.SetActive(false));
        }

        private static string Describe(LobbyError error) => error switch
        {
            LobbyError.InvalidPassword => "Wrong password",
            LobbyError.LobbyFull => "Lobby is full",
            LobbyError.LobbyNotFound => "Lobby not found",
            LobbyError.AlreadyInLobby => "Already in a lobby",
            LobbyError.DeckInvalid => "Deck is not valid",
            _ => "Something went wrong"
        };
    }
}
