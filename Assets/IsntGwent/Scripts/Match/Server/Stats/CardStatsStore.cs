using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Diagnostics;
using Mirror;
using Newtonsoft.Json;
using Zenject;

namespace IsntGwent.Scripts.Match.Server.Stats
{
    public class CardStatsStore : IInitializable
    {
        private const string FileName = "cards.json";
        private const string DumpName = "cards.txt";
        private const int MinMatchesForDump = 5;

        [Inject] private readonly LogService _logs;
        [Inject] private readonly CardDatabase _cards;

        private readonly Dictionary<string, CardStats> _stats = new();

        private string _directory;
        private int _matches;

        public IReadOnlyCollection<CardStats> All => _stats.Values;

        public int Matches => _matches;

        public void Initialize()
        {
            if (!NetworkServer.active) return;
            if (!_logs.IsEnabled) return;

            _directory = _logs.StatsDirectory;

            try
            {
                Directory.CreateDirectory(_directory);
            }
            catch (Exception e)
            {
                Log.Error(LogTag.Stats, "failed to create stats folder: " + e.Message);
                _directory = null;
                return;
            }

            Load();
        }

        public void Apply(GameContext context, MatchCardTally tally)
        {
            if (context == null || tally == null) return;

            _matches++;

            foreach (var pair in tally.Cards)
            {
                var entry = Of(pair.Key);

                entry.Played += pair.Value.Played;
                entry.Drawn += pair.Value.Drawn;
                entry.Redrawn += pair.Value.Redrawn;
                entry.Summoned += pair.Value.Summoned;
                entry.Kills += pair.Value.Kills;
                entry.Deaths += pair.Value.Deaths;
                entry.Damage += pair.Value.Damage;
            }

            ApplySide(context, tally, context.Player1);
            ApplySide(context, tally, context.Player2);

            Save();
        }

        private void ApplySide(GameContext context, MatchCardTally tally, Player player)
        {
            if (player == null) return;

            var isWinner = !context.IsTie && context.Winner == player;
            var played = tally.PlayedBy(player);

            foreach (var cardId in tally.DeckOf(player))
            {
                var entry = Of(cardId);

                entry.Matches++;

                if (context.IsTie) entry.Ties++;
                else if (isWinner) entry.Wins++;
                else entry.Losses++;

                if (!played.Contains(cardId)) continue;

                entry.PlayedMatches++;

                if (isWinner) entry.PlayedWins++;
            }
        }

        private CardStats Of(string cardId)
        {
            if (_stats.TryGetValue(cardId, out var entry)) return entry;

            entry = new CardStats { Id = cardId };
            _stats[cardId] = entry;

            return entry;
        }

        private void Load()
        {
            var path = Path.Combine(_directory, FileName);
            if (!File.Exists(path)) return;

            try
            {
                var file = JsonConvert.DeserializeObject<StatsFile>(File.ReadAllText(path));
                if (file?.Cards == null) return;

                _matches = file.Matches;

                foreach (var entry in file.Cards)
                {
                    if (!string.IsNullOrEmpty(entry.Id)) _stats[entry.Id] = entry;
                }

                Log.Info(LogTag.Stats, $"card stats loaded: {_stats.Count} cards, {_matches} matches");
            }
            catch (Exception e)
            {
                Log.Error(LogTag.Stats, "failed to read card stats: " + e.Message);
            }
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(_directory)) return;

            var file = new StatsFile
            {
                Matches = _matches,
                UpdatedAt = DateTime.Now.ToString("O"),
                Cards = _stats.Values.OrderBy(c => c.Id).ToArray(),
            };

            try
            {
                File.WriteAllText(Path.Combine(_directory, FileName),
                    JsonConvert.SerializeObject(file, Formatting.Indented));

                File.WriteAllText(Path.Combine(_directory, DumpName), Dump(), new UTF8Encoding(false));
            }
            catch (Exception e)
            {
                Log.Error(LogTag.Stats, "failed to save card stats: " + e.Message);
            }
        }

        private string Dump()
        {
            var text = new StringBuilder();

            text.AppendLine($"Статистика карт — {DateTime.Now:yyyy-MM-dd HH:mm}, партий: {_matches}");
            text.AppendLine($"Winrate считается по партиям, где карта была в колоде; " +
                            $"строки с колодой меньше {MinMatchesForDump} партий помечены звёздочкой.");
            text.AppendLine();
            text.AppendLine($"{"карта",-28}{"в колоде",9}{"win%",7}{"сыграна",9}{"play%",7}" +
                            $"{"win% игр",9}{"мулиган",9}{"убила",7}{"умерла",8}{"урон/игру",10}");

            foreach (var entry in _stats.Values.OrderByDescending(c => c.PlayedWinRate).ThenByDescending(c => c.Matches))
            {
                var mark = entry.Matches < MinMatchesForDump ? "*" : string.Empty;

                text.AppendLine($"{Name(entry.Id) + mark,-28}{entry.Matches,9}{entry.WinRate * 100,6:F0}%" +
                                $"{entry.Played,9}{entry.PlayRate * 100,6:F0}%{entry.PlayedWinRate * 100,8:F0}%" +
                                $"{entry.Redrawn,9}{entry.Kills,7}{entry.Deaths,8}{entry.DamagePerPlay,10:F1}");
            }

            return text.ToString();
        }

        private string Name(string cardId)
        {
            if (_cards == null || !_cards.Cards.TryGetValue(cardId, out var definition)) return cardId;

            return string.IsNullOrEmpty(definition.Name) ? cardId : $"{definition.Name} ({cardId})";
        }

        private class StatsFile
        {
            [JsonProperty("matches")] public int Matches;
            [JsonProperty("updatedAt")] public string UpdatedAt;
            [JsonProperty("cards")] public CardStats[] Cards;
        }
    }
}
