using IsntGwent.Scripts.Decks.Definitions;
using UniRx;

namespace IsntGwent.Scripts.Lobby.Client
{
    public class DeckSelectService
    {
        public readonly ReactiveProperty<DeckDefinition> SelectedDeck = new();
    }
}