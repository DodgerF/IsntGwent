using IsntGwent.Scripts.Lobby.Core;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct StartTutorialMessage : NetworkMessage
    {
    }

    public struct TutorialActionMessage : NetworkMessage
    {
        public string Action;
    }

    public struct TutorialStartResultMessage : NetworkMessage
    {
        public bool IsSuccess;
        public LobbyError Error;
        public string SeatToken;
    }
}
