using System;
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
                    foreach (var kvp in _registry.All)
                    {
                        var state = pool.Contains(kvp.Key.ToString())
                            ? CardView.TargetHighlightState.Available
                            : CardView.TargetHighlightState.None;
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
                        kvp.Value.SetTargetHighlight(CardView.TargetHighlightState.None);
                })
                .AddTo(_disposables);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
