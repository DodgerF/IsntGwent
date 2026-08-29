using System;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using Mirror;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Lobby.Client
{
    public class LobbyClientHandler : IInitializable, IDisposable
    {
        [Inject] private readonly MatchReconnectService _reconnect;

        public readonly Subject<(LobbyError error, DeckViolation[] violations)> OnError = new();
        public readonly Subject<Unit> OnJoinedLobby = new();
        public readonly Subject<Unit> OnLobbyCreated = new();

        public void Initialize()
        {
            if (NetworkClient.active)
            {
                NetworkClient.RegisterHandler<CreateLobbyResultMessage>(OnCreateLobbyResult);
                NetworkClient.RegisterHandler<JoinLobbyResultMessage>(OnJoinLobbyResult);
            }
        }

        public void SendCreateLobby(string lobbyName, string password, DeckDefinition deck)
        {
            NetworkClient.Send(new CreateLobbyMessage
            {
                Name =  lobbyName,
                Password = password,
                Deck = deck,
            });
        }

        public void SendJoinToLobby(string lobbyId, string password, DeckDefinition deck)
        {
            NetworkClient.Send(new JoinLobbyMessage
            {
                LobbyId = lobbyId,
                Password = password,
                Deck = deck,
            });
        }

        private void OnJoinLobbyResult(JoinLobbyResultMessage msg)
        {
            if (msg.IsSuccess)
            {
                _reconnect.BeginSeat(msg.SeatToken);
                OnJoinedLobby.OnNext(Unit.Default);
            }
            else
            {
                OnError.OnNext((msg.Error, msg.Violations));
            }
        }

        private void OnCreateLobbyResult(CreateLobbyResultMessage msg)
        {
            if (msg.IsSuccess)
            {
                _reconnect.BeginSeat(msg.SeatToken);
                OnLobbyCreated.OnNext(Unit.Default);
                OnJoinedLobby.OnNext(Unit.Default);
            }
            else
            {
                OnError.OnNext((msg.Error, msg.Violations));
            }
        }

        public void Dispose()
        {
            NetworkClient.UnregisterHandler<CreateLobbyResultMessage>();
            NetworkClient.UnregisterHandler<JoinLobbyResultMessage>();
        }
    }
}
