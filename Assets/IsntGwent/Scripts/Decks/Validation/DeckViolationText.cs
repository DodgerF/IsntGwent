using System.Collections.Generic;
using IsntGwent.Scripts.Localization;

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
                DeckViolationCode.DeckTooSmall => Loc.F("Not finished: {0} cards missing", violation.Expected - violation.Actual),
                DeckViolationCode.TooManyCopies => Loc.F("Too many copies of {0} (max {1})", violation.CardId, violation.Expected),
                DeckViolationCode.UnknownCard => Loc.F("Unknown card: {0}", violation.CardId),
                DeckViolationCode.BadCount => Loc.F("Bad card count: {0}", violation.CardId),
                DeckViolationCode.DuplicateEntry => Loc.F("Duplicate entry: {0}", violation.CardId),
                DeckViolationCode.ContentMismatch => Loc.T("Deck was built for another card set"),
                DeckViolationCode.MalformedDeck => violation.Field == "cards" ? Loc.T("Deck is empty") : Loc.T("Deck is malformed"),
                _ => Loc.T("Deck is malformed")
            };
        }
    }
}
