using System.Collections.Generic;

namespace IsntGwent.Scripts.Match.Server.Stats
{
    public class MatchCardTally
    {
        public class Counters
        {
            public int Played;
            public int Drawn;
            public int Redrawn;
            public int Summoned;
            public int Kills;
            public int Deaths;
            public int Damage;
        }

        private readonly Dictionary<string, Counters> _cards = new();
        private readonly Dictionary<Player, HashSet<string>> _decks = new();
        private readonly Dictionary<Player, HashSet<string>> _played = new();

        public IReadOnlyDictionary<string, Counters> Cards => _cards;

        public IReadOnlyCollection<string> DeckOf(Player player) => Side(_decks, player);

        public IReadOnlyCollection<string> PlayedBy(Player player) => Side(_played, player);

        public void Deck(Player player, IEnumerable<string> cardIds)
        {
            if (player == null || cardIds == null) return;

            var side = Side(_decks, player);

            foreach (var id in cardIds)
            {
                if (!string.IsNullOrEmpty(id)) side.Add(id);
            }
        }

        public void Played(Player player, string cardId)
        {
            if (string.IsNullOrEmpty(cardId)) return;

            Of(cardId).Played++;

            if (player != null) Side(_played, player).Add(cardId);
        }

        public void Drawn(string cardId)
        {
            if (!string.IsNullOrEmpty(cardId)) Of(cardId).Drawn++;
        }

        public void Redrawn(string cardId)
        {
            if (!string.IsNullOrEmpty(cardId)) Of(cardId).Redrawn++;
        }

        public void Summoned(string cardId)
        {
            if (!string.IsNullOrEmpty(cardId)) Of(cardId).Summoned++;
        }

        public void Killed(string cardId)
        {
            if (!string.IsNullOrEmpty(cardId)) Of(cardId).Kills++;
        }

        public void Died(string cardId)
        {
            if (!string.IsNullOrEmpty(cardId)) Of(cardId).Deaths++;
        }

        public void Damage(string cardId, int amount)
        {
            if (string.IsNullOrEmpty(cardId) || amount <= 0) return;

            Of(cardId).Damage += amount;
        }

        private Counters Of(string cardId)
        {
            if (_cards.TryGetValue(cardId, out var counters)) return counters;

            counters = new Counters();
            _cards[cardId] = counters;

            return counters;
        }

        private static HashSet<string> Side(Dictionary<Player, HashSet<string>> map, Player player)
        {
            if (player == null) return new HashSet<string>();

            if (map.TryGetValue(player, out var set)) return set;

            set = new HashSet<string>();
            map[player] = set;

            return set;
        }
    }
}
