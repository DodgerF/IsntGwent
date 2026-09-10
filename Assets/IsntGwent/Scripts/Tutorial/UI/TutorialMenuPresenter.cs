using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Accounts.Client;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Lobby.UI;
using IsntGwent.Scripts.Network;
using IsntGwent.Scripts.Tutorial.Client;
using IsntGwent.Scripts.Tutorial.Definitions;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Tutorial.UI
{
    public class TutorialMenuPresenter : MonoBehaviour
    {
        public const string OpenNicknameAction = "openNickname";

        private const float AnchorWait = 2.5f;

        [Inject] private readonly LobbyViewModel _lobby;
        [Inject] private readonly PlayerAccount _account;
        [Inject] private readonly ConnectionService _connection;
        [Inject] private readonly MatchReconnectService _reconnect;
        [Inject] private readonly InputRouter _input;
        [Inject] private readonly DeckSelectService _deckSelect;
        [Inject] private readonly TutorialScriptProvider _provider;
        [InjectOptional] private readonly TutorialService _tutorial;

        private readonly SerialDisposable _advance = new();
        private readonly SerialDisposable _launch = new();

        private TutorialOverlayView _overlay;
        private TutorialAnchors _anchors;

        private void Start()
        {
            if (_tutorial == null || AppRole.IsServer)
            {
                enabled = false;
                return;
            }

            Log.Info(LogTag.Tutorial, $"menu presenter up, state: {_tutorial.State.Value}");

            _overlay = FindFirstObjectByType<TutorialOverlayView>(FindObjectsInactive.Include)
                       ?? TutorialOverlayFactory.Create();
            _anchors = FindFirstObjectByType<TutorialAnchors>(FindObjectsInactive.Include);

            _advance.AddTo(this);
            _launch.AddTo(this);

            TutorialAura.SetHidden(false);

            var windows = TutorialWindows.Collect();

            Observable.EveryUpdate()
                .Select(_ => TutorialWindows.AnyOpen(windows))
                .DistinctUntilChanged()
                .Subscribe(TutorialAura.SetHidden)
                .AddTo(this);

            _tutorial.CurrentStep
                .SkipWhile(step => step == null)
                .Subscribe(OnStep)
                .AddTo(this);

            _tutorial.State
                .Skip(1)
                .Where(state => state == TutorialState.Menu)
                .Subscribe(_ => _tutorial.BeginMenuSteps())
                .AddTo(this);

            _tutorial.RestartRequested
                .Subscribe(_ => Relaunch())
                .AddTo(this);

            if (_tutorial.IsMenuPhase)
            {
                _tutorial.BeginMenuSteps();
                return;
            }

            if (_tutorial.IsDone)
            {
                Log.Info(LogTag.Tutorial, "tutorial is done, nothing to show");
                return;
            }

            if (_reconnect.HasSeat)
            {
                Log.Info(LogTag.Tutorial, "seat is held, waiting for the match to resume");

                _launch.Disposable = _reconnect.SeatDropped
                    .Take(1)
                    .Subscribe(_ => Launch());

                return;
            }

            Launch();
        }

        private void Relaunch()
        {
            Log.Info(LogTag.Tutorial, "tutorial restarted, back to the greeting");

            _advance.Disposable = null;

            TutorialAura.Clear();
            _overlay?.Hide();

            Launch();
        }

        private void Launch()
        {
            _launch.Disposable = _connection.IsConnected
                .CombineLatest(_provider.OnLoaded, (isConnected, isLoaded) => isConnected && isLoaded)
                .Where(isReady => isReady)
                .Take(1)
                .Subscribe(_ => BeginIntro());
        }

        private void BeginIntro()
        {
            Log.Info(LogTag.Tutorial, $"intro steps: {_provider.IntroSteps.Count}, state: {_tutorial.State.Value}");

            if (_provider.IntroSteps.Count == 0)
            {
                _tutorial.StartTutorial();
                return;
            }

            _tutorial.BeginIntroSteps();
        }

        private void OnStep(TutorialStep step)
        {
            _advance.Disposable = null;
            TutorialAura.Clear();

            if (!_tutorial.IsIntro && !_tutorial.IsMenuPhase)
            {
                _overlay?.Hide();
                return;
            }

            if (step == null)
            {
                _overlay?.Hide();

                if (_tutorial.IsIntro)
                    _tutorial.StartTutorial();
                else
                    _tutorial.CompleteMenu();

                return;
            }

            Log.Info(LogTag.Tutorial, $"menu step '{step.Id}', overlay: {(_overlay == null ? "none" : _overlay.name)}");

            RunAction(step.Action);

            var bag = new CompositeDisposable();

            _advance.Disposable = bag;

            bag.Add(WaitForAnchors(step.Highlight)
                .Take(1)
                .Subscribe(anchors => Present(step, anchors, bag)));
        }

        private IObservable<List<RectTransform>> WaitForAnchors(List<string> keys)
        {
            var wanted = keys?.Count ?? 0;

            if (Resolve(keys, out var anchors) >= wanted) return Observable.Return(anchors);

            return Observable.EveryUpdate()
                .Select(_ => Resolve(keys, out var found) >= wanted ? found : null)
                .Where(found => found != null)
                .Merge(Observable.Timer(TimeSpan.FromSeconds(AnchorWait))
                    .Select(_ =>
                    {
                        Resolve(keys, out var found);

                        return found;
                    }))
                .Take(1);
        }

        private void Present(TutorialStep step, List<RectTransform> anchors, CompositeDisposable bag)
        {
            if (step.Aura) TutorialAura.Apply(anchors);

            Resolve(step.Panel, out var seat);
            _overlay?.Show(step.Text, anchors, seat, true);

            bag.Add(Dismissed()
                .Take(1)
                .Subscribe(_ =>
                {
                    if (string.Equals(step.Advance, "tap", StringComparison.OrdinalIgnoreCase))
                    {
                        _overlay?.Hide();
                        TutorialAura.Clear();
                        _tutorial.Advance();
                        return;
                    }

                    BeginAction(step, anchors, bag);
                }));
        }

        private void BeginAction(TutorialStep step, List<RectTransform> anchors, CompositeDisposable bag)
        {
            _overlay?.Hide();

            if (step.Aura) TutorialAura.Apply(anchors);

            bag.Add(Trigger(step)
                .Take(1)
                .Subscribe(_ =>
                {
                    TutorialAura.Clear();
                    _tutorial.Advance();
                }));
        }

        private IObservable<Unit> Dismissed()
        {
            if (_overlay == null)
            {
                Log.Error(LogTag.Tutorial, "overlay is missing, the step has nothing to wait for");

                return Observable.Never<Unit>();
            }

            return _overlay.Tapped.SkipUntil(Observable.NextFrame());
        }

        private void RunAction(string action)
        {
            if (action == OpenNicknameAction)
                _lobby.OpenNicknameWindow();
        }

        private IObservable<Unit> Trigger(TutorialStep step)
        {
            var advance = step.Advance ?? "tap";

            if (advance.StartsWith("nickname", StringComparison.OrdinalIgnoreCase))
                return _account.OnLoggedIn;

            if (advance.StartsWith("deckSelected", StringComparison.OrdinalIgnoreCase))
                return _deckSelect.SelectedDeck.Skip(1).AsUnitObservable();

            return _input.Pressed.AsUnitObservable().Merge(_input.EmptyPressed);
        }

        private int Resolve(List<string> keys, out List<RectTransform> anchors)
        {
            anchors = new List<RectTransform>();

            if (keys == null) return 0;

            foreach (var key in keys)
            {
                var target = TutorialAnchors.Resolve(_anchors, key);

                if (target != null) anchors.Add(target);
            }

            return anchors.Count;
        }
    }
}
