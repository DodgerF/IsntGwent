using System;
using IsntGwent.Scripts.Decks.Validation;
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
        private static readonly TimeSpan ShowDuration = TimeSpan.FromSeconds(3);

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
                .Subscribe(e => Show(e.error, e.violations))
                .AddTo(this);
        }

        private void Show(LobbyError error, DeckViolation[] violations)
        {
            errorText.text = Describe(error, violations);
            panel.SetActive(true);

            _hideTimer.Disposable = Observable
                .Timer(ShowDuration)
                .Subscribe(_ => panel.SetActive(false));
        }

        private static string Describe(LobbyError error, DeckViolation[] violations)
        {
            if (error == LobbyError.DeckInvalid && violations is { Length: > 0 })
                return DeckViolationText.Describe(violations[0]);

            return error switch
            {
                LobbyError.InvalidCode => "Wrong room code",
                LobbyError.RoomFull => "Room is full",
                LobbyError.RoomNotFound => "Room not found",
                LobbyError.AlreadyInLobby => "Already in a room",
                LobbyError.NotLoggedIn => "Enter your nickname first",
                LobbyError.DeckInvalid => "Deck is not valid",
                _ => "Something went wrong"
            };
        }
    }
}
