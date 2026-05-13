using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Lobby.Services;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI
{
    public class LobbyViewModel
    {
        [Inject] private LobbyService _lobbyService;
        [Inject] private SessionService _sessionService;
        public readonly ReactiveProperty<bool> IsCreateLobbyWindowOpen = new(false);
        public readonly ReactiveProperty<bool> IsPasswordWindowOpen = new(false);
        private LobbyData? _selectedLobby;
        public IReadOnlyReactiveCollection<LobbyData> Lobbies => _lobbyService.Lobbies;
        
        public void CreateLobby(string name, string password)
        {
            _sessionService.SendCreateLobby(name, password);
        }

        public void JoinLobby(string lobbyId, string password)
        {
            _selectedLobby = null;
            _sessionService.SendJoinToLobby(lobbyId, password);
        }

        public void SelecteLobby(LobbyData lobby)
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
    }
}