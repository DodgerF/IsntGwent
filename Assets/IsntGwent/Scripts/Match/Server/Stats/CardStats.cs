using Newtonsoft.Json;

namespace IsntGwent.Scripts.Match.Server.Stats
{
    public class CardStats
    {
        [JsonProperty("id")] public string Id;

        [JsonProperty("matches")] public int Matches;
        [JsonProperty("wins")] public int Wins;
        [JsonProperty("losses")] public int Losses;
        [JsonProperty("ties")] public int Ties;

        [JsonProperty("playedMatches")] public int PlayedMatches;
        [JsonProperty("playedWins")] public int PlayedWins;

        [JsonProperty("played")] public int Played;
        [JsonProperty("drawn")] public int Drawn;
        [JsonProperty("redrawn")] public int Redrawn;
        [JsonProperty("summoned")] public int Summoned;
        [JsonProperty("kills")] public int Kills;
        [JsonProperty("deaths")] public int Deaths;
        [JsonProperty("damage")] public int Damage;

        [JsonIgnore] public float WinRate => Matches == 0 ? 0f : (float)Wins / Matches;

        [JsonIgnore] public float PlayedWinRate => PlayedMatches == 0 ? 0f : (float)PlayedWins / PlayedMatches;

        [JsonIgnore] public float PlayRate => Matches == 0 ? 0f : (float)PlayedMatches / Matches;

        [JsonIgnore] public float DamagePerPlay => Played == 0 ? 0f : (float)Damage / Played;
    }
}
