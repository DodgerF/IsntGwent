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
        public readonly ReactiveProperty<bool> IsLoginWindowOpen = new(false);
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
                .CombineLatest(_account.NeedsItchLogin, (canPlay, needsLogin) => canPlay && !needsLogin)
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

            // Окно входа открывается на том же месте, где раньше спрашивали ник: после обучения.
            // Пока обучение идёт, окно ведёт его сценарий (шаг с action openNickname), поэтому
            // отсюда оно открывается только у того, кто обучение уже прошёл.
            var loginWanted = _tutorial == null
                ? Observable.Return(true)
                : _tutorial.State.Select(state => state == TutorialState.Done);

            _account.NeedsItchLogin
                .CombineLatest(loginWanted, (needsLogin, isWanted) => (needsLogin, isWanted))
                .Subscribe(t =>
                {
                    if (t.needsLogin && t.isWanted) IsLoginWindowOpen.Value = true;
                    else if (!t.needsLogin) IsLoginWindowOpen.Value = false;
                })
                .AddTo(_disposables);

            _account.OnLoginFailed
                .Where(_ => WantsLogin)
                .Subscribe(_ => IsLoginWindowOpen.Value = true)
                .AddTo(_disposables);
        }

        private bool WantsLogin => _tutorial == null || _tutorial.IsDone;

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

        public void OpenLoginWindow()
        {
            IsLoginWindowOpen.Value = true;
        }

        public void CloseLoginWindow()
        {
            IsLoginWindowOpen.Value = false;
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
