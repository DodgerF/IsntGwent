using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IsntGwent.Scripts.Cards.Definitions
{
    public abstract class CardDefinition
    {
        public string Id;
        public string Name;
        public CardType Type;
        public string Description;
        public string ImageName;
        public string SoundId;
        public int MaxCopies;

        [JsonIgnore]
        public List<EffectDefinition> Effects = new();
        [JsonProperty("effects")]
        public List<JObject> RawEffects;
    }
}