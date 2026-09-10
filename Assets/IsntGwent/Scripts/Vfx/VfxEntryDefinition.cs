using Newtonsoft.Json;
using UnityEngine;

namespace IsntGwent.Scripts.Vfx
{
    public class VfxEntryDefinition
    {
        [JsonProperty("id")] public string Id;
        [JsonProperty("prefab")] public string Prefab;
        [JsonProperty("scale")] public float Scale = 1f;
        [JsonProperty("life")] public float Life = 1f;
        [JsonProperty("tint")] public string Tint;

        [JsonIgnore] public GameObject LoadedPrefab;
        [JsonIgnore] public float BaseScale = 1f;
        [JsonIgnore] public ParticleSystem.MinMaxGradient[] BaseColors;
        [JsonIgnore] public Vector3[] BaseShapes;
        [JsonIgnore] public bool HasTint;
        [JsonIgnore] public Color TintColor = Color.white;
    }

    public class VfxCatalogFile
    {
        [JsonProperty("entries")] public VfxEntryDefinition[] Entries;
    }
}
