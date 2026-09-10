using System;
using System.Collections.Generic;
using System.Linq;
using IsntGwent.Scripts.Cards.UI;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Cards.Client
{
    public class TargetHighlightPresenter : IInitializable, IDisposable
    {
        [Inject] private readonly CardSelectionService _selectionService;
        [Inject] private readonly CardViewRegistry _registry;

        private readonly CompositeDisposable _disposables = new();
        private readonly HashSet<string> _selected = new();

        private IReadOnlyList<string> _pool = Array.Empty<string>();
        private PlayPreviewQuery.Prediction _prediction = PlayPreviewQuery.Prediction.Empty;

        public void Initialize()
        {
            _selectionService.HighlightTargets
                .Subscribe(pool =>
                {
                    _pool = pool ?? (IReadOnlyList<string>)Array.Empty<string>();
                    _prediction = PlayPreviewQuery.Prediction.Empty;
                    _selected.Clear();
                    Repaint();
                })
                .AddTo(_disposables);

            _selectionService.HighlightPredicted
                .Subscribe(prediction =>
                {
                    _prediction = prediction;
                    Repaint();
                })
                .AddTo(_disposables);

            _selectionService.TargetSelected
                .Subscribe(id =>
                {
                    _selected.Add(id);
                    _registry.Get(id)?.SetTargetHighlight(CardView.TargetHighlightState.Selected);
                })
                .AddTo(_disposables);

            _selectionService.TargetDeselected
                .Subscribe(id =>
                {
                    _selected.Remove(id);
                    _registry.Get(id)?.SetTargetHighlight(CardView.TargetHighlightState.Available);
                })
                .AddTo(_disposables);

            _selectionService.ClearHighlights
                .Subscribe(_ =>
                {
                    _pool = Array.Empty<string>();
                    _prediction = PlayPreviewQuery.Prediction.Empty;
                    _selected.Clear();

                    foreach (var kvp in _registry.All)
                    {
                        kvp.Value.SetTargetHighlight(CardView.TargetHighlightState.None);
                        kvp.Value.SetDimmed(false);
                    }
                })
                .AddTo(_disposables);
        }

        private void Repaint()
        {
            var dimming = _pool.Count > 0;

            foreach (var kvp in _registry.All)
            {
                var id = kvp.Key.ToString();
                var view = kvp.Value;

                var inPool = _pool.Contains(id);
                var hostile = _prediction.Hostile.Contains(id);
                var friendly = !hostile && _prediction.Friendly.Contains(id);

                var state = CardView.TargetHighlightState.None;

                if (hostile) state = CardView.TargetHighlightState.PredictedHostile;
                else if (friendly) state = CardView.TargetHighlightState.PredictedFriendly;
                else if (_selected.Contains(id)) state = CardView.TargetHighlightState.Selected;
                else if (inPool) state = CardView.TargetHighlightState.Available;

                view.SetTargetHighlight(state);
                view.SetDimmed(dimming && !inPool && !hostile && !friendly && !IsSelectedCard(view));
            }
        }

        private bool IsSelectedCard(CardView view)
        {
            var selected = _selectionService.SelectedCard;

            return selected != null && view.Instance == selected;
        }

        public void Dispose() => _disposables.Dispose();
    }
}
