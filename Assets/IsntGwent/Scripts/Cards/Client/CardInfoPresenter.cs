using System;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks.UI;
using IsntGwent.Scripts.Match.Client;
using IsntGwent.Scripts.UI;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Cards.Client
{
    public class CardInfoPresenter : IInitializable, IDisposable
    {
        [Inject] private readonly InputRouter _inputRouter;
        [Inject] private readonly CardPreviewService _previewService;
        [InjectOptional] private readonly CardTooltipView _tooltip;
        [InjectOptional] private readonly MatchState _matchState;
        [InjectOptional] private readonly CardSelectionService _selectionService;

        private readonly CompositeDisposable _disposables = new();

        private bool _wasSelectingTargets;
        private CardView _hoveredCard;

        public void Initialize()
        {
            _inputRouter.Pressed
                .Subscribe(OnPressed)
                .AddTo(_disposables);

            _inputRouter.CardInspected
                .Where(CanShowPreview)
                .Subscribe(ShowPreview)
                .AddTo(_disposables);

            if (_selectionService == null)
                _inputRouter.CardPressed
                    .Where(CanShowPreview)
                    .Where(card => !HandlesGesturesItself(card))
                    .Subscribe(ShowPreview)
                    .AddTo(_disposables);

            _inputRouter.BoardCardPressed
                .Where(CanShowBoardPreview)
                .Subscribe(ShowPreview)
                .AddTo(_disposables);

            _inputRouter.CardHovered
                .Subscribe(ShowTooltip)
                .AddTo(_disposables);

            _inputRouter.HoverEnded
                .Subscribe(_ => HideTooltip())
                .AddTo(_disposables);

            if (_matchState == null) return;

            _matchState.IsGameEnded
                .Where(ended => ended)
                .Subscribe(_ => HideTooltip())
                .AddTo(_disposables);
        }

        private void OnPressed(GameObject pressed)
        {
            _wasSelectingTargets = _selectionService is { IsSelectingTargets: true };

            if (IsOutsidePreview(pressed))
                _previewService.Hide();
        }

        private static bool IsOutsidePreview(GameObject pressed)
            => pressed == null || pressed.GetComponentInParent<CardPreviewWindow>() == null;

        private void ShowPreview(CardView card)
        {
            HideTooltip();
            _previewService.ShowModal(card.Instance);
        }

        private bool CanShowPreview(CardView card)
        {
            if (card == null || card.Instance == null) return false;

            return _matchState == null || !_matchState.IsRedrawPhase.Value;
        }

        private bool CanShowBoardPreview(CardView card)
            => !_wasSelectingTargets && CanShowPreview(card);

        private static bool HandlesGesturesItself(CardView card)
            => card.GetComponentInParent<HoldButton>() is { isActiveAndEnabled: true };

        private void ShowTooltip(CardView card)
        {
            if (card == null || card.Instance == null) return;
            if (HandlesGesturesItself(card)) return;

            SetHovered(card);

            _tooltip?.Show(card.Instance.Definition, (RectTransform)card.transform);
        }

        private void HideTooltip()
        {
            SetHovered(null);
            _tooltip?.Hide();
        }

        private void SetHovered(CardView card)
        {
            if (_hoveredCard == card) return;

            if (_hoveredCard != null)
                _hoveredCard.SetHovered(false);

            _hoveredCard = card;

            if (_hoveredCard != null)
                _hoveredCard.SetHovered(true);
        }

        public void Dispose() => _disposables.Dispose();
    }
}
