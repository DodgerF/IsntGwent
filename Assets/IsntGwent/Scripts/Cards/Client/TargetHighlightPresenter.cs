using System;
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

        public void Initialize()
        {
            _selectionService.HighlightTargets
                .Subscribe(pool =>
                {
                    var dimming = pool.Count > 0;

                    foreach (var kvp in _registry.All)
                    {
                        var id = kvp.Key.ToString();
                        var isTarget = pool.Contains(id);

                        kvp.Value.SetTargetHighlight(isTarget
                            ? CardView.TargetHighlightState.Available
                            : CardView.TargetHighlightState.None);

                        kvp.Value.SetDimmed(dimming && !isTarget && !IsSelected(kvp.Value));
                    }
                })
                .AddTo(_disposables);

            _selectionService.HighlightPredicted
                .Subscribe(prediction =>
                {
                    foreach (var kvp in _registry.All)
                    {
                        var id = kvp.Key.ToString();

                        var state = CardView.TargetHighlightState.None;
                        if (prediction.Hostile.Contains(id))
                            state = CardView.TargetHighlightState.PredictedHostile;
                        else if (prediction.Friendly.Contains(id))
                            state = CardView.TargetHighlightState.PredictedFriendly;

                        kvp.Value.SetTargetHighlight(state);
                    }
                })
                .AddTo(_disposables);

            _selectionService.TargetSelected
                .Subscribe(id => _registry.Get(id)?.SetTargetHighlight(CardView.TargetHighlightState.Selected))
                .AddTo(_disposables);

            _selectionService.TargetDeselected
                .Subscribe(id => _registry.Get(id)?.SetTargetHighlight(CardView.TargetHighlightState.Available))
                .AddTo(_disposables);

            _selectionService.ClearHighlights
                .Subscribe(_ =>
                {
                    foreach (var kvp in _registry.All)
                    {
                        kvp.Value.SetTargetHighlight(CardView.TargetHighlightState.None);
                        kvp.Value.SetDimmed(false);
                    }
                })
                .AddTo(_disposables);
        }

        private bool IsSelected(CardView view)
        {
            var selected = _selectionService.SelectedCard;

            return selected != null && view.Instance == selected;
        }

        public void Dispose() => _disposables.Dispose();
    }
}
