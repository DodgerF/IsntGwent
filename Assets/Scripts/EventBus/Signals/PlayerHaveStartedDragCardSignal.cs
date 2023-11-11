namespace IsntGwent
{
    public class PlayerHaveStartedDragCardSignal
    {
        private Card _card;
        public Card Card { get { return _card; } }
        public PlayerHaveStartedDragCardSignal(Card card) 
        { 
            _card = card;
        }
    }
}