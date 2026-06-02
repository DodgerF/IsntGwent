using IsntGwent.Scripts.Cards.Definitions;
using UniRx;

namespace IsntGwent.Scripts.Cards.Services
{
    public class CardPreviewService  
    {  
        public readonly Subject<CardDefinition> ShowCard = new();  
        public readonly Subject<Unit> HideCard = new();  
    }
}