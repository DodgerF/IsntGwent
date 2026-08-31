using System.Collections.Generic;
using Newtonsoft.Json;

namespace IsntGwent.Scripts.Match.Server.Journal
{
    public class MatchJournalEvent
    {
        [JsonProperty("seq")] public int Seq;
        [JsonProperty("time")] public string Time;
        [JsonProperty("ms")] public long Ms;
        [JsonProperty("round")] public int Round;
        [JsonProperty("turn")] public int Turn;
        [JsonProperty("type")] public string Type;
        [JsonProperty("actor")] public string Actor;
        [JsonProperty("text")] public string Text;
        [JsonProperty("data")] public Dictionary<string, object> Data;
    }
}
