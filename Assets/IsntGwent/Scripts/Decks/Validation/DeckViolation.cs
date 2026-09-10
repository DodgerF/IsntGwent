namespace IsntGwent.Scripts.Decks.Validation
{
    public enum DeckViolationCode
    {
        MalformedDeck,
        ContentMismatch,
        UnknownCard,
        DuplicateEntry,
        BadCount,
        TooManyCopies,
        DeckTooSmall
    }

    public struct DeckViolation
    {
        public DeckViolationCode Code;
        public string CardId;
        public string Field;
        public int Expected;
        public int Actual;
    }

    public static class DeckViolationCodes
    {
        public static string ToWire(DeckViolationCode code)
        {
            return code switch
            {
                DeckViolationCode.MalformedDeck => "MALFORMED_DECK",
                DeckViolationCode.ContentMismatch => "CONTENT_MISMATCH",
                DeckViolationCode.UnknownCard => "UNKNOWN_CARD",
                DeckViolationCode.DuplicateEntry => "DUPLICATE_ENTRY",
                DeckViolationCode.BadCount => "BAD_COUNT",
                DeckViolationCode.TooManyCopies => "TOO_MANY_COPIES",
                DeckViolationCode.DeckTooSmall => "DECK_TOO_SMALL",
                _ => "UNKNOWN"
            };
        }
    }
}
