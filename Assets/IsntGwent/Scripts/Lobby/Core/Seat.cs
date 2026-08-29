using System;
using IsntGwent.Scripts.Decks.Definitions;
using Mirror;

namespace IsntGwent.Scripts.Lobby.Core
{
    public class Seat
    {
        public readonly string Id = Guid.NewGuid().ToString();
        public readonly string Token = Guid.NewGuid().ToString();
        public readonly DeckDefinition Deck;

        public ISeatChannel Channel;
        public bool IsReady;
        public bool IsConnected = true;

        public Seat(DeckDefinition deck, ISeatChannel channel)
        {
            Deck = deck;
            Channel = channel;
        }

        public void Send<T>(T message) where T : struct, NetworkMessage
        {
            if (!IsConnected) return;
            if (Channel == null) return;

            Channel.Send(message);
        }
    }
}
