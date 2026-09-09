using System;
using IsntGwent.Scripts.Accounts.Client;
using IsntGwent.Scripts.Decks;
using IsntGwent.Scripts.Decks.Validation;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Lobby.Core;
using IsntGwent.Scripts.Network;
using IsntGwent.Scripts.Tutorial.Client;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Lobby.UI
{
    public class LobbyViewModel : IInitializable, IDisposable
    {
        [Inject] private LobbyClientHandler _handler;
        [Inject] private DeckSelectService _deckSelect;
        [Inject] private DeckRulesProvider _rules;
        [Inject] private DeckValidator _validator;
        [Inject] private ConnectionService _connection;
        [Inject] private PlayerAccount _account;
        [InjectOptional] private TutorialService _tutorial;

        public readonly ReactiveProperty<bool> IsJoinCodeWindowOpen = new(false);
        public readonly ReactiveProperty<bool> IsNicknameWindowOpen = new(false);
        public readonly ReactiveProperty<bool> IsSearching = new(false);
        public readonly ReactiveProperty<bool> CanPlay = new(false);

        private readonly CompositeDisposable _disposables = new();

        private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
        private readonly ReactiveProperty<bool> _isRequestPending = new(false);
        private readonly SerialDisposable _requestTimeout = new();

        public void Initialize()
        {
            _requestTimeout.AddTo(_disposables);

            var tutorialPassed = _tutorial == null
                ? Observable.Return(true)
                : _tutorial.State.Select(state => state == TutorialState.Done);

            _deckSelect.SelectedDeck
                .CombineLatest(_connection.IsConnected, _isRequestPending, _rules.OnLoaded, _account.IsLoggedIn,
                    (deck, isConnected, isPending, rulesLoaded, isLoggedIn) =>
                        deck != null && isConnected && !isPending && rulesLoaded && isLoggedIn &&
                        _validator.IsValid(deck))
                .CombineLatest(tutorialPassed, (canPlay, isPassed) => canPlay && isPassed)
                .Subscribe(canPlay => CanPlay.Value = canPlay)
                .AddTo(_disposables);

            _handler.OnJoinedLobby
                .Subscribe(_ => ClearPending())
                .AddTo(_disposables);

            _handler.OnSearchStarted
                .Subscribe(_ =>
                {
                    ClearPending();
                    IsSearching.Value = true;
                })
                .AddTo(_disposables);

            _handler.OnError
                .Subscribe(_ =>
                {
                    ClearPending();
                    IsSearching.Value = false;
                })
                .AddTo(_disposables);

            _connection.IsConnected
                .Where(isConnected => !isConnected)
                .Subscribe(_ => IsSearching.Value = false)
                .AddTo(_disposables);

            _account.IsLoggedIn
                .Where(isLoggedIn => isLoggedIn)
                .Select(_ => Unit.Default)
                .Merge(_account.OnLoggedIn)
                .Subscribe(_ => IsNicknameWindowOpen.Value = false)
                .AddTo(_disposables);

            _account.OnLoginFailed
                .Where(_ => WantsNickname)
                .Subscribe(_ => IsNicknameWindowOpen.Value = true)
                .AddTo(_disposables);

            if (!_account.HasNickname && WantsNickname)
                IsNicknameWindowOpen.Value = true;
        }

        private bool WantsNickname => _tutorial == null || _tutorial.WantsNickname;

        public void TogglePlay()
        {
            if (IsSearching.Value)
                CancelSearch();
            else
                FindMatch();
        }

        public void FindMatch()
        {
            _handler.SendFindMatch(_deckSelect.SelectedDeck.Value);
            BeginPending();
        }

        public void CancelSearch()
        {
            IsSearching.Value = false;
            _handler.SendCancelSearch();
        }

        public void CreatePrivateRoom()
        {
            _handler.SendCreatePrivateRoom(_deckSelect.SelectedDeck.Value);
            BeginPending();
        }

        public void OpenJoinCodeWindow()
        {
            IsJoinCodeWindowOpen.Value = true;
        }

        public void CloseJoinCodeWindow()
        {
            IsJoinCodeWindowOpen.Value = false;
        }

        public void ConfirmJoinByCode(string code)
        {
            _handler.SendJoinByCode(JoinCodes.Normalize(code), _deckSelect.SelectedDeck.Value);
            BeginPending();

            IsJoinCodeWindowOpen.Value = false;
        }

        public void OpenNicknameWindow()
        {
            IsNicknameWindowOpen.Value = true;
        }

        public void CloseNicknameWindow()
        {
            IsNicknameWindowOpen.Value = false;
        }

        private void BeginPending()
        {
            _isRequestPending.Value = true;
            _requestTimeout.Disposable = Observable
                .Timer(RequestTimeout)
                .Subscribe(_ => _isRequestPending.Value = false);
        }

        private void ClearPending()
        {
            _requestTimeout.Disposable = null;
            _isRequestPending.Value = false;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
