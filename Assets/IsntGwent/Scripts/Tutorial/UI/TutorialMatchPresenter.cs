using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Diagnostics;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Tutorial.Client;
using IsntGwent.Scripts.Tutorial.Definitions;
using IsntGwent.Scripts.Tutorial.Server;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Tutorial.UI
{
    public class TutorialMatchPresenter : MonoBehaviour
    {
        private const float WarningDuration = 4.5f;
        private const float AnchorWait = 2.5f;

        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _views;
        [InjectOptional] private readonly TutorialService _tutorial;

        private readonly SerialDisposable _advance = new();
        private readonly SerialDisposable _auraRefresh = new();
        private readonly Dictionary<string, RectTransform> _static = new();

        private TutorialOverlayView _overlay;
        private TutorialAnchors _anchors;
        private BoardRowView[] _rows;
        private HandOutlineView _handAura;

        private void Start()
        {
            if (_tutorial == null || !_tutorial.IsMatchPhase)
            {
                enabled = false;
                return;
            }

            _overlay = FindFirstObjectByType<TutorialOverlayView>(FindObjectsInactive.Include)
                       ?? TutorialOverlayFactory.Create();
            _anchors = FindFirstObjectByType<TutorialAnchors>(FindObjectsInactive.Include);

            _advance.AddTo(this);
            _auraRefresh.AddTo(this);

            BindHandAura();
            BindWindows();

            _tutorial.CurrentStep
                .Subscribe(OnStep)
                .AddTo(this);

            _matchState.IsWaitingImageActive
                .Where(isWaiting => !isWaiting)
                .Take(1)
                .Subscribe(_ => StartSteps())
                .AddTo(this);

            _tutorial.PassRejected
                .Subscribe(warning => _overlay?.ShowMessage(warning, WarningDuration))
                .AddTo(this);

            _matchState.IsGameEnded
                .Where(isEnded => isEnded)
                .Take(1)
                .Subscribe(_ => OnGameEnded())
                .AddTo(this);

            _matchState.IsGameEnded
                .CombineLatest(_matchState.AmIWinner, (isEnded, amIWinner) => isEnded && amIWinner)
                .Where(isWon => isWon)
                .Take(1)
                .Subscribe(_ => _tutorial.CompleteMatch())
                .AddTo(this);
        }

        private void StartSteps()
        {
            _tutorial.BeginMatchSteps(IsSatisfied);

            if (_tutorial.CurrentStep.Value == null)
                ReleaseHandAura();
        }

        private void BindWindows()
        {
            TutorialAura.SetHidden(false);

            var windows = TutorialWindows.Collect();

            Observable.EveryUpdate()
                .Select(_ => TutorialWindows.AnyOpen(windows))
                .DistinctUntilChanged()
                .Subscribe(TutorialAura.SetHidden)
                .AddTo(this);
        }

        private void BindHandAura()
        {
            _handAura = FindFirstObjectByType<HandOutlineView>(FindObjectsInactive.Include);

            if (_handAura == null) return;

            _handAura.SetSuppressed(true);

            _tutorial.CurrentStep
                .SkipWhile(step => step == null)
                .Where(step => step == null)
                .Take(1)
                .Subscribe(_ => ReleaseHandAura())
                .AddTo(this);
        }

        private void ReleaseHandAura()
        {
            if (_handAura == null) return;

            _handAura.SetSuppressed(false);
        }

        private void OnGameEnded()
        {
            _advance.Disposable = null;
            _auraRefresh.Disposable = null;
            TutorialAura.Clear();
            _overlay?.Hide();

            if (!_matchState.AmIGiveUp.Value)
                _tutorial.CompleteMatch();
        }

        private void OnStep(TutorialStep step)
        {
            _advance.Disposable = null;
            _auraRefresh.Disposable = null;
            TutorialAura.Clear();

            if (step == null)
            {
                _overlay?.Hide();
                return;
            }

            RunAction(step.Action);

            var bag = new CompositeDisposable();

            _advance.Disposable = bag;

            var trigger = Latch(step, bag);

            bag.Add(WaitForAnchors(step.Highlight)
                .Take(1)
                .Subscribe(anchors => Present(step, anchors, trigger, bag)));
        }

        private IObservable<Unit> Latch(TutorialStep step, CompositeDisposable bag)
        {
            var advance = step.Advance ?? "tap";

            if (advance.StartsWith("tap", StringComparison.OrdinalIgnoreCase)) return null;
            if (advance.StartsWith("delay", StringComparison.OrdinalIgnoreCase)) return null;

            var latched = Trigger(step).Take(1).PublishLast();

            bag.Add(latched.Connect());

            return latched;
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

        private void Present(TutorialStep step, List<RectTransform> anchors, IObservable<Unit> trigger,
            CompositeDisposable bag)
        {
            if (string.IsNullOrEmpty(step.Text))
            {
                BeginAction(step, anchors, trigger, bag);
                return;
            }

            ApplyAura(step, anchors);

            Resolve(step.Panel, out var seat);
            _overlay?.Show(step.Text, anchors, seat, true);

            bag.Add(Dismissed()
                .Take(1)
                .Subscribe(_ =>
                {
                    if (trigger == null && string.Equals(step.Advance, "tap", StringComparison.OrdinalIgnoreCase))
                    {
                        _tutorial.Advance();
                        return;
                    }

                    BeginAction(step, anchors, trigger, bag);
                }));
        }

        private void BeginAction(TutorialStep step, List<RectTransform> anchors, IObservable<Unit> trigger,
            CompositeDisposable bag)
        {
            _overlay?.Hide();

            ApplyAura(step, anchors);

            bag.Add((trigger ?? Trigger(step))
                .Take(1)
                .Subscribe(_ =>
                {
                    TutorialAura.Clear();
                    _tutorial.Advance();
                }));
        }

        private void ApplyAura(TutorialStep step, List<RectTransform> anchors)
        {
            if (!step.Aura)
            {
                TutorialAura.Clear();
                return;
            }

            TutorialAura.Apply(anchors);

            if (!IsLive(step.Highlight)) return;

            var current = anchors;

            _auraRefresh.Disposable = Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    Resolve(step.Highlight, out var found);

                    if (Same(current, found)) return;

                    current = found;
                    TutorialAura.Apply(found);
                });
        }

        private static bool IsLive(List<string> keys)
        {
            if (keys == null) return false;

            foreach (var key in keys)
            {
                if (key == null) continue;

                if (key.StartsWith("hand:", StringComparison.OrdinalIgnoreCase)) return true;
                if (key.StartsWith("board:", StringComparison.OrdinalIgnoreCase)) return true;
                if (key.StartsWith("cell:", StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }

        private static bool Same(List<RectTransform> left, List<RectTransform> right)
        {
            if (left.Count != right.Count) return false;

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i]) return false;
            }

            return true;
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
            if (string.IsNullOrEmpty(action)) return;

            if (action == TutorialDirector.DealAction)
                _tutorial.SendAction(action);
        }

        private IObservable<Unit> Trigger(TutorialStep step)
        {
            var trigger = StepTrigger(step);

            return _tutorial.IsResuming ? trigger.Merge(Stale()) : trigger;
        }

        private IObservable<Unit> Stale()
        {
            return _matchState.LastRoundResult
                .Skip(1)
                .Where(result => result != RoundResult.None)
                .AsUnitObservable()
                .Merge(_matchState.IsGameEnded.Where(isEnded => isEnded).AsUnitObservable());
        }

        private bool IsSatisfied(TutorialStep step)
        {
            var advance = step?.Advance;

            if (string.IsNullOrEmpty(advance)) return false;

            var value = Argument(advance);

            if (advance.StartsWith("cardPlayed", StringComparison.OrdinalIgnoreCase))
                return !_matchState.Hand.Any(card => Matches(card, value));

            if (advance.StartsWith("enemyPlayed", StringComparison.OrdinalIgnoreCase))
                return _matchState.EnemyMeleeRow.Units
                    .Concat(_matchState.EnemyRangedRow.Units)
                    .Concat(_matchState.EnemyGraveyard)
                    .Any(card => Matches(card, value));

            return false;
        }

        private IObservable<Unit> StepTrigger(TutorialStep step)
        {
            var advance = step.Advance ?? "tap";
            var value = Argument(advance);

            if (advance.StartsWith("cardPlayed", StringComparison.OrdinalIgnoreCase))
                return _matchState.Hand
                    .ObserveRemove()
                    .Where(e => Matches(e.Value, value))
                    .AsUnitObservable();

            if (advance.StartsWith("enemyPlayed", StringComparison.OrdinalIgnoreCase))
                return _matchState.EnemyMeleeRow.Placed
                    .Merge(_matchState.EnemyRangedRow.Placed)
                    .Where(e => Matches(e.Card, value))
                    .AsUnitObservable();

            if (advance.StartsWith("pass", StringComparison.OrdinalIgnoreCase))
                return _matchState.PassSent;

            if (advance.StartsWith("turnStart", StringComparison.OrdinalIgnoreCase))
                return _matchState.IsMyTurn.Skip(1).Where(isMine => isMine).AsUnitObservable();

            if (advance.StartsWith("roundEnd", StringComparison.OrdinalIgnoreCase))
                return _matchState.LastRoundResult
                    .Skip(1)
                    .Where(result => result != RoundResult.None)
                    .AsUnitObservable();

            if (advance.StartsWith("gameEnd", StringComparison.OrdinalIgnoreCase))
                return _matchState.IsGameEnded.Where(isEnded => isEnded).AsUnitObservable();

            if (advance.StartsWith("delay", StringComparison.OrdinalIgnoreCase))
            {
                var seconds = float.TryParse(value, out var parsed) ? parsed : 1f;
                return Observable.Timer(TimeSpan.FromSeconds(seconds)).AsUnitObservable();
            }

            return Observable.Never<Unit>();
        }

        private static string Argument(string advance)
        {
            var index = advance.IndexOf(':');

            return index < 0 ? string.Empty : advance[(index + 1)..];
        }

        private static bool Matches(CardInstance card, string definitionId)
        {
            if (card?.Definition == null) return false;

            return string.IsNullOrEmpty(definitionId) || card.Definition.Id == definitionId;
        }

        private int Resolve(List<string> keys, out List<RectTransform> anchors)
        {
            anchors = new List<RectTransform>();

            if (keys == null) return 0;

            var resolved = 0;

            foreach (var key in keys)
            {
                var before = anchors.Count;

                ResolveKey(key, anchors);

                if (anchors.Count > before) resolved++;
            }

            return resolved;
        }

        private void ResolveKey(string key, List<RectTransform> anchors)
        {
            if (string.IsNullOrEmpty(key)) return;

            var value = Argument(key);

            if (key.StartsWith("hand:", StringComparison.OrdinalIgnoreCase))
            {
                AddViews(_matchState.Hand.Where(c => Matches(c, value)), anchors);
                return;
            }

            if (key.StartsWith("board:", StringComparison.OrdinalIgnoreCase))
            {
                AddViews(BoardCards().Where(c => Matches(c, value)), anchors);
                return;
            }

            if (key.StartsWith("cell:", StringComparison.OrdinalIgnoreCase))
            {
                AddCells(value, anchors);
                return;
            }

            if (key.StartsWith("row:", StringComparison.OrdinalIgnoreCase))
            {
                Add(anchors, RowOf(value));
                return;
            }

            if (key.StartsWith("slot:", StringComparison.OrdinalIgnoreCase))
            {
                Add(anchors, SlotOf(value));
                return;
            }

            if (!_static.TryGetValue(key, out var anchor) || anchor == null)
            {
                anchor = TutorialAnchors.Resolve(_anchors, key);
                _static[key] = anchor;
            }

            Add(anchors, anchor);
        }

        private void AddCells(string definitionId, List<RectTransform> anchors)
        {
            AddCells(_matchState.OwnMeleeRow, true, RowType.Melee, definitionId, anchors);
            AddCells(_matchState.OwnRangedRow, true, RowType.Ranged, definitionId, anchors);
            AddCells(_matchState.EnemyMeleeRow, false, RowType.Melee, definitionId, anchors);
            AddCells(_matchState.EnemyRangedRow, false, RowType.Ranged, definitionId, anchors);
        }

        private void AddCells(BoardRowState state, bool ownSide, RowType row, string definitionId,
            List<RectTransform> anchors)
        {
            var view = FindRow(TutorialService.RowKey(ownSide, row));

            if (view == null) return;

            for (var i = 0; i < state.Slots.Length; i++)
            {
                if (!Matches(state.Slots[i], definitionId)) continue;

                Add(anchors, view.SlotTransform(i));
            }
        }

        private void AddViews(IEnumerable<CardInstance> cards, List<RectTransform> anchors)
        {
            foreach (var card in cards)
            {
                Add(anchors, ViewOf(card));
            }
        }

        private static void Add(List<RectTransform> anchors, RectTransform target)
        {
            if (target != null) anchors.Add(target);
        }

        private RectTransform ViewOf(CardInstance card)
        {
            if (card == null) return null;

            var view = _views.Get(card.Id);

            return view == null ? null : view.VisualRect;
        }

        private IEnumerable<CardInstance> BoardCards()
        {
            return _matchState.OwnMeleeRow.Units
                .Concat(_matchState.OwnRangedRow.Units)
                .Concat(_matchState.EnemyMeleeRow.Units)
                .Concat(_matchState.EnemyRangedRow.Units);
        }

        private RectTransform RowOf(string key)
        {
            var row = FindRow(key);

            return row == null ? null : (RectTransform)row.transform;
        }

        private RectTransform SlotOf(string key)
        {
            var parts = key.Split(':');

            if (parts.Length < 2) return null;
            if (!int.TryParse(parts[1], out var index)) return null;

            var row = FindRow(parts[0]);

            return row == null ? null : row.SlotTransform(index);
        }

        private BoardRowView FindRow(string key)
        {
            _rows ??= FindObjectsByType<BoardRowView>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var row in _rows)
            {
                if (row == null || !row.HasSlots) continue;

                if (TutorialService.RowKey(row.OwnSide, row.BoardRow) == key) return row;
            }

            return null;
        }
    }
}
