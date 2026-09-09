using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Lobby.Client;
using IsntGwent.Scripts.Network;
using IsntGwent.Scripts.Tutorial.Definitions;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Tutorial.Client
{
    public enum TutorialState
    {
        None,
        Match,
        Menu,
        Done,
    }

    public class TutorialService : IInitializable, IDisposable
    {
        private const string StateKey = "tutorial.state";
        private const string StepKey = "tutorial.step";
        private const string NicknameKey = "account.nick";

        [Inject] private readonly TutorialScriptProvider _provider;
        [Inject] private readonly LobbyClientHandler _handler;

        public readonly ReactiveProperty<TutorialStep> CurrentStep = new();
        public readonly ReactiveProperty<TutorialState> State = new(TutorialState.None);
        public readonly Subject<string> PassRejected = new();
        public readonly Subject<Unit> RestartRequested = new();

        private readonly CompositeDisposable _disposables = new();

        private List<TutorialStep> _steps = new();
        private int _index;
        private bool _isMatchSteps;

        public bool IsDone => State.Value == TutorialState.Done;
        public bool IsMatchPhase => State.Value == TutorialState.Match;
        public bool IsMenuPhase => State.Value == TutorialState.Menu;
        public bool IsIntro { get; private set; }
        public bool IsResuming { get; private set; }
        public bool WantsNickname => State.Value is TutorialState.Menu or TutorialState.Done;

        public void Initialize()
        {
            if (AppRole.IsServer) return;

            var saved = PlayerPrefs.GetString(StateKey, string.Empty);

            if (string.IsNullOrEmpty(saved))
            {
                var hasNickname = !string.IsNullOrEmpty(PlayerPrefs.GetString(NicknameKey, string.Empty));

                if (hasNickname) SetState(TutorialState.Done);

                return;
            }

            State.Value = Parse(saved);

            MyNetManager.ClientDisconnected
                .Where(_ => IsMatchPhase)
                .Subscribe(_ => SuspendSteps())
                .AddTo(_disposables);
        }

        public void StartTutorial()
        {
            SetState(TutorialState.Match);
            ResetSteps();

            _handler.SendStartTutorial();

            Log.Info(LogTag.Tutorial, "tutorial requested");
        }

        public void BeginIntroSteps()
        {
            IsIntro = true;
            _isMatchSteps = false;

            BeginSteps(_provider.IntroSteps);
        }

        public void BeginMatchSteps(Func<TutorialStep, bool> isSatisfied = null)
        {
            IsIntro = false;
            _isMatchSteps = true;

            var resume = Mathf.Max(0, PlayerPrefs.GetInt(StepKey, 0));

            BeginSteps(_provider.Steps, resume, resume > 0 ? isSatisfied : null);
        }

        public void BeginMenuSteps()
        {
            IsIntro = false;
            _isMatchSteps = false;

            BeginSteps(_provider.MenuSteps);
        }

        public void SendAction(string action)
        {
            if (string.IsNullOrEmpty(action)) return;

            _handler.SendTutorialAction(action);
        }

        public void Advance()
        {
            if (_index >= _steps.Count) return;

            _index++;
            IsResuming = false;

            SaveProgress();

            CurrentStep.Value = _index < _steps.Count ? _steps[_index] : null;
        }

        public void CompleteMatch()
        {
            if (!IsMatchPhase) return;

            SetState(TutorialState.Menu);
            ResetSteps();

            Log.Info(LogTag.Tutorial, "tutorial match finished");
        }

        public void BeginMenuPhase()
        {
            SetState(TutorialState.Menu);
            ResetSteps();
        }

        public void CompleteMenu()
        {
            if (!IsMenuPhase) return;

            SetState(TutorialState.Done);
            ResetSteps();

            Log.Info(LogTag.Tutorial, "tutorial finished");
        }

        public void MarkDone()
        {
            SetState(TutorialState.Done);
            ResetSteps();
        }

        public void Restart()
        {
            SetState(TutorialState.None);
            ResetSteps();

            RestartRequested.OnNext(Unit.Default);
        }

        public bool CanSelectCard(string definitionId)
        {
            if (!IsMatchPhase) return true;

            var step = CurrentStep.Value;

            if (step == null) return true;

            return step.Allow?.Cards != null && step.Allow.Cards.Contains(definitionId);
        }

        public bool CanPlaceAt(bool ownSide, RowType row, int slotIndex)
        {
            var allow = Allow;

            if (allow == null) return true;

            if (allow.Rows != null && allow.Rows.Count > 0 && !allow.Rows.Contains(RowKey(ownSide, row)))
                return false;

            return allow.Slots == null || allow.Slots.Count == 0 || allow.Slots.Contains(slotIndex);
        }

        public bool CanTarget(string definitionId)
        {
            var allow = Allow;

            return allow?.Targets == null || allow.Targets.Contains(definitionId);
        }

        public bool EvaluatePass(bool handEmpty, bool enemyPassed, bool leading)
        {
            if (!IsMatchPhase) return true;

            var step = CurrentStep.Value;

            if (step != null) return step.Allow != null && step.Allow.Pass;

            return handEmpty || (enemyPassed && leading);
        }

        public void RejectPass()
        {
            var warning = _provider.Script?.PassWarning;

            if (string.IsNullOrEmpty(warning)) return;

            PassRejected.OnNext(warning);
        }

        public static string RowKey(bool ownSide, RowType row) => (ownSide ? "Own" : "Enemy") + row;

        private TutorialAllow Allow => IsMatchPhase ? CurrentStep.Value?.Allow : null;

        private void BeginSteps(IReadOnlyList<TutorialStep> steps, int from = 0,
            Func<TutorialStep, bool> isSatisfied = null)
        {
            _steps = steps == null ? new List<TutorialStep>() : new List<TutorialStep>(steps);
            _index = Mathf.Clamp(from, 0, _steps.Count);

            while (isSatisfied != null && _index < _steps.Count && isSatisfied(_steps[_index]))
                _index++;

            IsResuming = from > 0 && _index < _steps.Count;

            if (IsResuming)
                Log.Info(LogTag.Tutorial, $"tutorial steps resumed at {_index} of {_steps.Count}");

            SaveProgress();

            CurrentStep.Value = _index < _steps.Count ? _steps[_index] : null;
        }

        private void SaveProgress()
        {
            if (!_isMatchSteps) return;

            PlayerPrefs.SetInt(StepKey, _index);
            PlayerPrefs.Save();
        }

        private void ResetSteps()
        {
            SuspendSteps();

            PlayerPrefs.DeleteKey(StepKey);
            PlayerPrefs.Save();
        }

        private void SuspendSteps()
        {
            IsIntro = false;
            IsResuming = false;
            _isMatchSteps = false;

            _steps = new List<TutorialStep>();
            _index = 0;

            CurrentStep.Value = null;
        }

        private void SetState(TutorialState state)
        {
            State.Value = state;

            PlayerPrefs.SetString(StateKey, state.ToString());
            PlayerPrefs.Save();
        }

        private static TutorialState Parse(string value)
        {
            return Enum.TryParse(value, out TutorialState state) ? state : TutorialState.None;
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
