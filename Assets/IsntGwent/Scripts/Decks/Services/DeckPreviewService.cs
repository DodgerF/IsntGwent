using IsntGwent.Scripts.Decks.Definitions;
using UniRx;

namespace IsntGwent.Scripts
{
    public class DeckPreviewService
    {
        public readonly Subject<DeckDefinition> OnDeckSelected = new();
    }
}