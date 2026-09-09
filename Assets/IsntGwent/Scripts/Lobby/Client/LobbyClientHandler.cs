using System;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Network;
using Mirror;
using UniRx;
using UnityEngine.SceneManagement;
using Zenject;

namespace IsntGwent.Scripts.Lobby.Client
{
    public class LobbyClientHandler : IInitializable, IDisposable
    {
        private const string GameSceneName = "GameScene";

        [Inject] private readonly MatchReconnectService _reconnect;
        [Inject] private readonly PlaySession _play;

        public readonly Subject<(LobbyError error, DeckViolation[] violations)> OnError = new();
        public readonly Subject<Unit> OnJoinedLobby = new();
        public readonly Subject<Unit> OnSearchStarted = new();
        public readonly Subject<string> OnPrivateRoomCreated = new();
        public readonly Subject<Unit> OnMatchFound = new();

        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            MyNetManager.ClientConnected
                .Subscribe(_ => RegisterHandlers())
                .AddTo(_disposables);

            if (NetworkClient.active)
                RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            NetworkClient.ReplaceHandler<SearchStartedMessage>(OnSearchResult);
            NetworkClient.ReplaceHandler<MatchFoundMessage>(OnMatchFoundResult);
            NetworkClient.ReplaceHandler<PrivateRoomCreatedMessage>(OnPrivateRoomResult);
            NetworkClient.ReplaceHandler<JoinByCodeResultMessage>(OnJoinByCodeResult);
            NetworkClient.ReplaceHandler<TutorialStartResultMessage>(OnTutorialStartResult);
        }

        public void SendFindMatch(DeckDefinition deck)
        {
            NetworkClient.Send(new FindMatchMessage { Deck = deck });
        }

        public void SendStartTutorial()
        {
            NetworkClient.Send(new StartTutorialMessage());
        }

        public void SendTutorialAction(string action)
        {
            NetworkClient.Send(new TutorialActionMessage { Action = action });
        }

        public void SendCancelSearch()
        {
            NetworkClient.Send(new CancelSearchMessage());
        }

        public void SendCreatePrivateRoom(DeckDefinition deck)
        {
            NetworkClient.Send(new CreatePrivateRoomMessage { Deck = deck });
        }

        public void SendJoinByCode(string code, DeckDefinition deck)
        {
            NetworkClient.Send(new JoinByCodeMessage { Code = code, Deck = deck });
        }

        private void OnSearchResult(SearchStartedMessage msg)
        {
            if (!msg.IsSuccess)
            {
                OnError.OnNext((msg.Error, msg.Violations));
                return;
            }

            _play.BeginSearch();
            OnSearchStarted.OnNext(Unit.Default);
        }

        private void OnMatchFoundResult(MatchFoundMessage msg)
        {
            _play.BeginMatch();
            _reconnect.BeginSeat(msg.SeatToken);

            OnMatchFound.OnNext(Unit.Default);

            if (SceneManager.GetActiveScene().name == GameSceneName)
            {
                NetworkClient.Send(new Messages.ReadyMessage());
                return;
            }

            OnJoinedLobby.OnNext(Unit.Default);
        }

        private void OnPrivateRoomResult(PrivateRoomCreatedMessage msg)
        {
            if (!msg.IsSuccess)
            {
                OnError.OnNext((msg.Error, msg.Violations));
                return;
            }

            _play.BeginRoom(msg.JoinCode);
            _reconnect.BeginSeat(msg.SeatToken);

            OnPrivateRoomCreated.OnNext(msg.JoinCode);
            OnJoinedLobby.OnNext(Unit.Default);
        }

        private void OnTutorialStartResult(TutorialStartResultMessage msg)
        {
            if (!msg.IsSuccess)
            {
                OnError.OnNext((msg.Error, Array.Empty<DeckViolation>()));
                return;
            }

            _play.BeginMatch();
            _reconnect.BeginSeat(msg.SeatToken);

            OnJoinedLobby.OnNext(Unit.Default);
        }

        private void OnJoinByCodeResult(JoinByCodeResultMessage msg)
        {
            if (!msg.IsSuccess)
            {
                OnError.OnNext((msg.Error, msg.Violations));
                return;
            }

            _play.BeginMatch();
            _reconnect.BeginSeat(msg.SeatToken);

            OnJoinedLobby.OnNext(Unit.Default);
        }

        public void Dispose()
        {
            _disposables.Dispose();

            NetworkClient.UnregisterHandler<SearchStartedMessage>();
            NetworkClient.UnregisterHandler<MatchFoundMessage>();
            NetworkClient.UnregisterHandler<PrivateRoomCreatedMessage>();
            NetworkClient.UnregisterHandler<JoinByCodeResultMessage>();
            NetworkClient.UnregisterHandler<TutorialStartResultMessage>();
        }
    }
}
