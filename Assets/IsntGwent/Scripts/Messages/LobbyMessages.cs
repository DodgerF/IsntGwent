using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.Core;
using Mirror;

namespace IsntGwent.Scripts.Messages
{
    public struct FindMatchMessage : NetworkMessage
    {
        public DeckDefinition Deck;
    }

    public struct CancelSearchMessage : NetworkMessage
    {
    }

    public struct CreatePrivateRoomMessage : NetworkMessage
    {
        public DeckDefinition Deck;
    }

    public struct JoinByCodeMessage : NetworkMessage
    {
        public string Code;
        public DeckDefinition Deck;
    }

    public struct SearchStartedMessage : NetworkMessage
    {
        public bool IsSuccess;
        public LobbyError Error;
        public DeckViolation[] Violations;
    }

    public struct MatchFoundMessage : NetworkMessage
    {
        public string SeatToken;
    }

    public struct PrivateRoomCreatedMessage : NetworkMessage
    {
        public bool IsSuccess;
        public LobbyError Error;
        public DeckViolation[] Violations;
        public string SeatToken;
        public string JoinCode;
    }

    public struct JoinByCodeResultMessage : NetworkMessage
    {
        public bool IsSuccess;
        public LobbyError Error;
        public DeckViolation[] Violations;
        public string SeatToken;
    }
}
