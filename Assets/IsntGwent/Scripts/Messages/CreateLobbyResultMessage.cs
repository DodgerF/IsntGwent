using IsntGwent.Scripts.Lobby.Core;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct CreateLobbyResultMessage : NetworkMessage
    {
        public bool IsSuccess;
        public LobbyError Error;
        public string LobbyId;
    }
}