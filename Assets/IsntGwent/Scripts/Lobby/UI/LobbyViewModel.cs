using System;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Network;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI
{
    public class LobbyViewModel : IInitializable, IDisposable
    {
        [Inject] private LobbyStore _lobbyStore;
        [Inject] private LobbyClientHandler _handler;
        [Inject] private DeckSelectService _deckSelect;
        public readonly ReactiveProperty<bool> IsCreateLobbyWindowOpen = new(false);
        public readonly ReactiveProperty<bool> IsPasswordWindowOpen = new(false);
        public readonly ReactiveProperty<bool> CanCreateOrJoinLobby = new(false);
        private LobbyData? _selectedLobby;
        public IReadOnlyReactiveCollection<LobbyData> Lobbies => _lobbyStore.Lobbies;
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        
        public void Initialize()
        {
            _deckSelect.SelectedDeck
                .Subscribe(deck => CanCreateOrJoinLobby.Value = deck != null)
                .AddTo(_disposables);
        }
        
        public void CreateLobby(string name, string password)
        {
            _handler.SendCreateLobby(name, password, _deckSelect.SelectedDeck.Value);
        }

        public void JoinLobby(string lobbyId, string password)
        {
            _selectedLobby = null;
            _handler.SendJoinToLobby(lobbyId, password, _deckSelect.SelectedDeck.Value);
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