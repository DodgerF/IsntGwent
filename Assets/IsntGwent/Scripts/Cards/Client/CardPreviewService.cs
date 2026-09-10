using IsntGwent.Scripts.Cards.Runtime;
using UniRx;

namespace IsntGwent.Scripts.Cards.Client
{
    public readonly struct PreviewRequest
    {
        public readonly CardInstance Card;
        public readonly bool Modal;

        public PreviewRequest(CardInstance card, bool modal)
        {
            Card = card;
            Modal = modal;
        }
    }

    public class CardPreviewService
    {
        public readonly Subject<PreviewRequest> ShowCard = new();
        public readonly Subject<Unit> HideCard = new();

        public void Show(CardInstance card) => ShowCard.OnNext(new PreviewRequest(card, false));

        public void ShowModal(CardInstance card) => ShowCard.OnNext(new PreviewRequest(card, true));

        public void Hide() => HideCard.OnNext(Unit.Default);
    }
}
