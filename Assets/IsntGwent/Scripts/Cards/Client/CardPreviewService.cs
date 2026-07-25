using System;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Core;
using UniRx;
using Zenject;

namespace IsntGwent.Scripts.Cards.Client
{
    public class CardPreviewService : IInitializable, IDisposable
    {
        [Inject] private readonly InputRouter _inputRouter;

        public readonly Subject<CardInstance> ShowCard = new();
        public readonly Subject<Unit> HideCard = new();

        private readonly CompositeDisposable _disposables = new();

        public void Initialize()
        {
            _inputRouter.CardHovered
                .Subscribe(card => ShowCard.OnNext(card.Instance))
                .AddTo(_disposables);

            _inputRouter.HoverEnded
                .Subscribe(_ => HideCard.OnNext(Unit.Default))
                .AddTo(_disposables);
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}
