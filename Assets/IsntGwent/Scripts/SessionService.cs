using IsntGwent.Scripts.Messages;
using Mirror;
using UniRx;

namespace IsntGwent.Scripts
{
    public class SessionService
    {
        public readonly ReactiveProperty<string> CurrentLobbyId = new(""); 
        
        public readonly Subject<string> OnError = new();
        public readonly Subject<Unit> OnJoinedLobby = new();

        public void SendCreateLobby(string name, string password)
        {
            NetworkClient.Send(new CreateLobbyMessage
            {
                Name = name,
                Password = password
            });
        }

        public void SendJoinToLobby(string lobbyId, string password)
        {
            NetworkClient.Send(new JoinLobbyMessage
            {
                LobbyId = lobbyId,
                Password = password
            });
        }
    }
}