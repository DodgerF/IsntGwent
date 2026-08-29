using System.Collections.Generic;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Match.Server;
using IsntGwent.Scripts.Messages;

namespace IsntGwent.Scripts.Network.Http
{
    public class GatewaySession
    {
        private const int RememberedIntents = 32;

        public string SessionId;
        public string PlayerId;
        public string Name;
        public string Uuid;
        public long ExpiresAt;

        public HttpSeatChannel Channel = new();
        public Seat Seat;

        public RoundResult RoundResult;
        public MatchOutcome Outcome;
        public MatchStateWire LastState;

        private readonly Dictionary<string, IntentAcceptedWire> _intents = new();
        private readonly Queue<string> _intentOrder = new();

        public bool TryGetIntent(string intentId, out IntentAcceptedWire answer)
        {
            answer = null;

            return !string.IsNullOrEmpty(intentId) && _intents.TryGetValue(intentId, out answer);
        }

        public void RememberIntent(string intentId, IntentAcceptedWire answer)
        {
            if (string.IsNullOrEmpty(intentId)) return;
            if (_intents.ContainsKey(intentId)) return;

            _intents[intentId] = answer;
            _intentOrder.Enqueue(intentId);

            while (_intentOrder.Count > RememberedIntents)
                _intents.Remove(_intentOrder.Dequeue());
        }
    }
}
