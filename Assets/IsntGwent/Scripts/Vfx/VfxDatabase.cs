using System.Collections;
using System.Collections.Generic;
using Coffee.UIExtensions;
using IsntGwent.Scripts.Core;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Vfx
{
    public class VfxDatabase : IInitializable
    {
        public readonly Dictionary<string, VfxEntryDefinition> Entries = new();
        public ReactiveProperty<bool> OnLoaded = new();

        private readonly CoroutineRunner _runner;

        public VfxDatabase(CoroutineRunner runner) => _runner = runner;

        public VfxEntryDefinition Get(string id)
        {
            return id != null && Entries.TryGetValue(id, out var entry) ? entry : null;
        }

        public void Initialize()
        {
            _runner.StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            yield return StreamingAssetsLoader.LoadAllJson(
                folder: "Vfx",
                onComplete: jsonList =>
                {
                    foreach (var json in jsonList)
                        ParseAndAdd(json);

                    OnLoaded.Value = true;
                },
                onError: err => Debug.LogError(err)
            );
        }

        private void ParseAndAdd(string json)
        {
            json = json.TrimStart('﻿', '​');
            var catalog = JsonConvert.DeserializeObject<VfxCatalogFile>(json);
            if (catalog?.Entries == null) return;

            foreach (var entry in catalog.Entries)
            {
                if (string.IsNullOrEmpty(entry.Id)) continue;

                entry.LoadedPrefab = LoadPrefab(entry.Prefab);
                if (entry.LoadedPrefab != null)
                    Snapshot(entry);

                if (!string.IsNullOrEmpty(entry.Tint) && ColorUtility.TryParseHtmlString(entry.Tint, out var tint))
                {
                    entry.HasTint = true;
                    entry.TintColor = tint;
                }

                Entries[entry.Id] = entry;
            }
        }

        private static GameObject LoadPrefab(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var prefab = Resources.Load<GameObject>("Vfx/" + name);
            if (prefab == null)
                Debug.LogWarning("Vfx prefab not found: Vfx/" + name);

            return prefab;
        }

        private static void Snapshot(VfxEntryDefinition entry)
        {
            var particle = entry.LoadedPrefab.GetComponent<UIParticle>();
            entry.BaseScale = particle != null ? particle.scale : 1f;

            var systems = entry.LoadedPrefab.GetComponentsInChildren<ParticleSystem>(true);
            entry.BaseColors = new ParticleSystem.MinMaxGradient[systems.Length];
            entry.BaseShapes = new Vector3[systems.Length];
            for (var i = 0; i < systems.Length; i++)
            {
                entry.BaseColors[i] = systems[i].main.startColor;
                entry.BaseShapes[i] = systems[i].shape.scale;
            }
        }
    }
}
