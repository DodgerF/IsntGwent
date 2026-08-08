using System;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Network;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI
{
    public class LobbyViewModel : IInitializable, IDisposable
    {
        [Inject] private LobbyStore _lobbyStore;
        [Inject] private LobbyClientHandler _handler;
        [Inject] private DeckSelectService _deckSelect;
        [Inject] private DeckRulesProvider _rules;
        [Inject] private DeckValidator _validator;
        [Inject] private ConnectionService _connection;
        public readonly ReactiveProperty<bool> IsCreateLobbyWindowOpen = new(false);
        public readonly ReactiveProperty<bool> IsPasswordWindowOpen = new(false);
        public readonly ReactiveProperty<bool> CanCreateOrJoinLobby = new(false);
        private LobbyData? _selectedLobby;
        public IReadOnlyReactiveCollection<LobbyData> Lobbies => _lobbyStore.Lobbies;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
        private readonly ReactiveProperty<bool> _isRequestPending = new(false);
        private readonly SerialDisposable _requestTimeout = new();

        public void Initialize()
        {
            _requestTimeout.AddTo(_disposables);

            _deckSelect.SelectedDeck
                .CombineLatest(_connection.IsConnected, _isRequestPending, _rules.OnLoaded,
                    (deck, isConnected, isPending, rulesLoaded) =>
                        deck != null && isConnected && !isPending && rulesLoaded && _validator.IsValid(deck))
                .Subscribe(canCreateOrJoin => CanCreateOrJoinLobby.Value = canCreateOrJoin)
                .AddTo(_disposables);

            _handler.OnJoinedLobby
                .Subscribe(_ => ClearPending())
                .AddTo(_disposables);

            _handler.OnError
                .Subscribe(_ => ClearPending())
                .AddTo(_disposables);
        }

        public void CreateLobby(string name, string password)
        {
            _handler.SendCreateLobby(name, password, _deckSelect.SelectedDeck.Value);
            BeginPending();
        }

        public void JoinLobby(string lobbyId, string password)
        {
            _selectedLobby = null;
            _handler.SendJoinToLobby(lobbyId, password, _deckSelect.SelectedDeck.Value);
            BeginPending();
        }

        private void BeginPending()
        {
            _isRequestPending.Value = true;
            _requestTimeout.Disposable = Observable
                .Timer(RequestTimeout)
                .Subscribe(_ => _isRequestPending.Value = false);
        }

        private void ClearPending()
        {
            _requestTimeout.Disposable = null;
            _isRequestPending.Value = false;
        }

        public void SelectLobby(LobbyData lobby)
        {
            _selectedLobby = lobby;

            if (lobby.IsPrivate)
            {
                IsPasswordWindowOpen.Value = true;
            }
            else
            {
                JoinLobby(lobby.LobbyId, "");
            }
        }

        public void ClosePasswordWindow()
        {
            IsPasswordWindowOpen.Value = false;
        }
        public void ConfirmJoinWithPassword(string password)
        {
            if (_selectedLobby == null)
                return;

            JoinLobby(_selectedLobby.Value.LobbyId, password);
            
            IsPasswordWindowOpen.Value = false;
        }
        

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}