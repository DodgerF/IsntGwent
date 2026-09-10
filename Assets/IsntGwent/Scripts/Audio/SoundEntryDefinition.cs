using Newtonsoft.Json;
using UnityEngine;

namespace IsntGwent.Scripts.Audio
{
    public class SoundEntryDefinition
    {
        public string Id;
        public SoundCategory Category;
        public string[] Clips;
        public float Volume = 1f;
        public float PitchMin = 1f;
        public float PitchMax = 1f;
        public bool Loop;

        [JsonIgnore]
        public AudioClip[] LoadedClips;
    }
}
