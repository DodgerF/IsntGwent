using IsntGwent.Scripts.Decks.Definitions;
using UniRx;

namespace IsntGwent.Scripts.Lobby.Client
{
    public class DeckSelectService
    {
        public readonly ReactiveProperty<DeckDefinition> SelectedDeck = new();

        private DeckDefinition _editTarget;
        private bool _editTargetIsBuiltIn;

        public void RequestEdit(DeckDefinition deck, bool isBuiltIn)
        {
            _editTarget = deck;
            _editTargetIsBuiltIn = isBuiltIn;
        }

        public void RequestNewDeck()
        {
            _editTarget = null;
            _editTargetIsBuiltIn = false;
        }

        public DeckDefinition ConsumeEditTarget(out bool isBuiltIn)
        {
            var deck = _editTarget;
            isBuiltIn = _editTargetIsBuiltIn;

            _editTarget = null;
            _editTargetIsBuiltIn = false;

            return deck;
        }
    }
}
