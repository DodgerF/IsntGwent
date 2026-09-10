using System.Collections.Generic;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks.Definitions;

namespace IsntGwent.Scripts.Decks.Validation
{
    public class DeckValidator
    {
        private static readonly DeckViolation[] Empty = new DeckViolation[0];

        private readonly CardDatabase _cards;
        private readonly DeckRulesProvider _rules;

        public DeckValidator(CardDatabase cards, DeckRulesProvider rules)
        {
            _cards = cards;
            _rules = rules;
        }

        public bool IsValid(DeckDefinition deck)
        {
            return Validate(deck).Count == 0;
        }

        public IReadOnlyList<DeckViolation> Validate(DeckDefinition deck)
        {
            var malformed = CheckMalformed(deck);
            if (malformed.HasValue) return new[] { malformed.Value };

            var rules = _rules.Current;

            if (deck.ContentVersion != rules.ContentVersion)
                return new[]
                {
                    new DeckViolation
                    {
                        Code = DeckViolationCode.ContentMismatch,
                        Expected = rules.ContentVersion,
                        Actual = deck.ContentVersion
                    }
                };

            var violations = new List<DeckViolation>();
            var order = new List<string>();
            var entryCounts = new Dictionary<string, int>();
            var cardCounts = new Dictionary<string, int>();

            foreach (var entry in deck.Cards)
            {
                if (!entryCounts.ContainsKey(entry.CardId))
                {
                    order.Add(entry.CardId);
                    entryCounts[entry.CardId] = 0;
                    cardCounts[entry.CardId] = 0;
                }

                entryCounts[entry.CardId]++;

                if (entry.Count >= 1)
                    cardCounts[entry.CardId] += entry.Count;
            }

            foreach (var cardId in order)
            {
                if (!_cards.Cards.TryGetValue(cardId, out var known) || known.IsToken)
                    violations.Add(new DeckViolation { Code = DeckViolationCode.UnknownCard, CardId = cardId });
            }

            foreach (var cardId in order)
            {
                if (entryCounts[cardId] > 1)
                    violations.Add(new DeckViolation
                    {
                        Code = DeckViolationCode.DuplicateEntry,
                        CardId = cardId,
                        Actual = entryCounts[cardId]
                    });
            }

            foreach (var entry in deck.Cards)
            {
                if (entry.Count < 1)
                    violations.Add(new DeckViolation
                    {
                        Code = DeckViolationCode.BadCount,
                        CardId = entry.CardId,
                        Actual = entry.Count
                    });
            }

            var size = 0;

            foreach (var cardId in order)
            {
                if (!_cards.Cards.TryGetValue(cardId, out var card) || card.IsToken) continue;

                var count = cardCounts[cardId];
                if (count < 1) continue;

                size += count;

                var max = _rules.MaxCopiesFor(card);
                if (count > max)
                    violations.Add(new DeckViolation
                    {
                        Code = DeckViolationCode.TooManyCopies,
                        CardId = cardId,
                        Expected = max,
                        Actual = count
                    });
            }

            if (size < rules.MinDeckSize)
                violations.Add(new DeckViolation
                {
                    Code = DeckViolationCode.DeckTooSmall,
                    Expected = rules.MinDeckSize,
                    Actual = size
                });

            return violations.Count == 0 ? Empty : violations;
        }

        private static DeckViolation? CheckMalformed(DeckDefinition deck)
        {
            if (deck == null) return Malformed("deck");
            if (string.IsNullOrEmpty(deck.Id)) return Malformed("id");
            if (string.IsNullOrEmpty(deck.Name)) return Malformed("name");
            if (deck.Cards == null || deck.Cards.Length == 0) return Malformed("cards");

            foreach (var entry in deck.Cards)
            {
                if (entry == null || string.IsNullOrEmpty(entry.CardId))
                    return Malformed("cardId");
            }

            return null;
        }

        private static DeckViolation Malformed(string field)
        {
            return new DeckViolation { Code = DeckViolationCode.MalformedDeck, Field = field };
        }
    }
}
