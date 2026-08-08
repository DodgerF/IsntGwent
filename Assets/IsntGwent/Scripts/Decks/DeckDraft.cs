using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Decks.Definitions;
using UniRx;

namespace IsntGwent.Scripts.Decks
{
    public class DeckDraft
    {
        private const string DefaultName = "New Deck";

        private readonly CardDatabase _cards;
        private readonly DeckRulesProvider _rules;

        public readonly ReactiveProperty<string> Name = new(DefaultName);
        public readonly ReactiveDictionary<string, int> Cards = new();
        public readonly ReactiveProperty<int> TotalCount = new();
        public readonly Subject<Unit> Changed = new();

        public string Id { get; private set; } = Guid.NewGuid().ToString();

        private string _savedSignature;

        public DeckDraft(CardDatabase cards, DeckRulesProvider rules)
        {
            _cards = cards;
            _rules = rules;
        }

        public void NewDeck()
        {
            Id = Guid.NewGuid().ToString();
            Name.Value = DefaultName;
            Cards.Clear();
            RefreshTotal();
            MarkSaved();
        }

        public void LoadFrom(DeckDefinition deck, bool asNewCopy = false)
        {
            if (deck == null)
            {
                NewDeck();
                return;
            }

            Cards.Clear();

            Id = asNewCopy || string.IsNullOrEmpty(deck.Id) ? Guid.NewGuid().ToString() : deck.Id;

            var name = string.IsNullOrEmpty(deck.Name) ? DefaultName : deck.Name;
            Name.Value = asNewCopy ? name + " copy" : name;

            if (deck.Cards != null)
            {
                foreach (var entry in deck.Cards)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.CardId)) continue;
                    if (!_cards.Cards.ContainsKey(entry.CardId)) continue;

                    var count = Math.Min(entry.Count, MaxCopiesOf(entry.CardId));
                    if (count < 1) continue;

                    Cards[entry.CardId] = CountOf(entry.CardId) + count;
                }
            }

            RefreshTotal();
            MarkSaved();
        }

        public bool IsDirty => _savedSignature != Signature();

        public void MarkSaved()
        {
            _savedSignature = Signature();
        }

        private string Signature()
        {
            var entries = new List<string>();
            foreach (var pair in Cards)
                entries.Add(pair.Key + ":" + pair.Value);

            entries.Sort(StringComparer.Ordinal);

            return Id + "|" + Name.Value + "|" + string.Join(";", entries);
        }

        public int CountOf(string cardId)
        {
            return Cards.TryGetValue(cardId, out var count) ? count : 0;
        }

        public int MaxCopiesOf(string cardId)
        {
            return _cards.Cards.TryGetValue(cardId, out var card) ? _rules.MaxCopiesFor(card) : 0;
        }

        public int RemainingOf(string cardId)
        {
            return Math.Max(0, MaxCopiesOf(cardId) - CountOf(cardId));
        }

        public bool TryAdd(string cardId)
        {
            if (RemainingOf(cardId) <= 0) return false;

            Cards[cardId] = CountOf(cardId) + 1;
            RefreshTotal();
            return true;
        }

        public bool Remove(string cardId)
        {
            var count = CountOf(cardId);
            if (count <= 0) return false;

            if (count == 1)
                Cards.Remove(cardId);
            else
                Cards[cardId] = count - 1;

            RefreshTotal();
            return true;
        }

        public DeckDefinition Build()
        {
            var entries = new List<DeckCardEntry>();

            foreach (var pair in Cards)
                entries.Add(new DeckCardEntry { CardId = pair.Key, Count = pair.Value });

            return new DeckDefinition
            {
                Id = Id,
                Name = string.IsNullOrEmpty(Name.Value) ? DefaultName : Name.Value,
                ContentVersion = _rules.Current.ContentVersion,
                Cards = entries.ToArray()
            };
        }

        private void RefreshTotal()
        {
            var total = 0;
            foreach (var pair in Cards)
                total += pair.Value;

            TotalCount.Value = total;
            Changed.OnNext(Unit.Default);
        }
    }
}
