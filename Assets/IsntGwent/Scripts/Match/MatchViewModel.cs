using System;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    public class MatchViewModel : IInitializable, IDisposable
    {
        [Inject] private readonly MatchClientHandler _handler;
        public readonly ReactiveProperty<bool> IsWaitingImageActive = new(true);
        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            _handler.OnGameStarted
                .Subscribe(_ => IsWaitingImageActive.Value = false)
                .AddTo(_disposables);
        }

        public void Ready()
        {
            _handler.SendReadyMessage();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}