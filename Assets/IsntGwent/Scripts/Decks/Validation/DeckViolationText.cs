using System.Collections.Generic;

namespace IsntGwent.Scripts.Decks.Validation
{
    public static class DeckViolationText
    {
        public static string DescribeFirst(IReadOnlyList<DeckViolation> violations)
        {
            return violations == null || violations.Count == 0 ? string.Empty : Describe(violations[0]);
        }

        public static string Describe(DeckViolation violation)
        {
            return violation.Code switch
            {
                DeckViolationCode.DeckTooSmall => "Not finished: " + (violation.Expected - violation.Actual) + " cards missing",
                DeckViolationCode.TooManyCopies => "Too many copies of " + violation.CardId + " (max " + violation.Expected + ")",
                DeckViolationCode.UnknownCard => "Unknown card: " + violation.CardId,
                DeckViolationCode.BadCount => "Bad card count: " + violation.CardId,
                DeckViolationCode.DuplicateEntry => "Duplicate entry: " + violation.CardId,
                DeckViolationCode.ContentMismatch => "Deck was built for another card set",
                DeckViolationCode.MalformedDeck => violation.Field == "cards" ? "Deck is empty" : "Deck is malformed",
                _ => "Deck is malformed"
            };
        }
    }
}
