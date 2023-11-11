namespace IsntGwent
{
    public class PlayerHavePlayedCardSignal
    {
        private Card _card;
        public Card Card { get { return _card; } }

        private Row _row;
        public Row Row { get { return _row; } }

        public PlayerHavePlayedCardSignal(Card card, Row row)
        {
            _card = card;
            _row = row;
        }

    }
}
