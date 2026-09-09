using System;
using IsntGwent.Scripts.Accounts.Core;
using IsntGwent.Scripts.Decks.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Lobby.Core
{
    public class Seat
    {
        public readonly string Id = Guid.NewGuid().ToString();
        public readonly string Token = Guid.NewGuid().ToString();
        public readonly DeckDefinition Deck;
        public readonly AccountData Account;

        public ISeatChannel Channel;
        public bool IsReady;
        public bool IsConnected = true;
        public bool IsBot;

        public Seat(DeckDefinition deck, ISeatChannel channel, AccountData account)
        {
            Deck = deck;
            Channel = channel;
            Account = account;
        }

        public void Send<T>(T message) where T : struct, NetworkMessage
        {
            if (!IsConnected) return;
            if (Channel == null) return;

            Channel.Send(message);
        }
    }
}
