using System;
using System.Collections;
using System.Collections.Generic;
using IsntGwent.Scripts.Core;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Audio
{
    public class SoundDatabase : IInitializable
    {
        public readonly Dictionary<string, SoundEntryDefinition> Entries = new();
        public ReactiveProperty<bool> OnLoaded = new();

        private readonly CoroutineRunner _runner;

        public SoundDatabase(CoroutineRunner runner) => _runner = runner;

        public SoundEntryDefinition Get(string id)
        {
            return Entries.TryGetValue(id, out var entry) ? entry : null;
        }

        public void Initialize()
        {
            _runner.StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            yield return StreamingAssetsLoader.LoadAllJson(
                folder: "Sounds",
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
            var catalog = JsonConvert.DeserializeObject<SoundCatalogFile>(json);
            if (catalog?.Entries == null) return;

            foreach (var entry in catalog.Entries)
            {
                if (string.IsNullOrEmpty(entry.Id)) continue;
                entry.LoadedClips = LoadClips(entry.Clips);
                Entries[entry.Id] = entry;
            }
        }

        private static AudioClip[] LoadClips(string[] names)
        {
            if (names == null) return Array.Empty<AudioClip>();

            var clips = new List<AudioClip>(names.Length);
            foreach (var name in names)
            {
                var clip = Resources.Load<AudioClip>("Sounds/" + name);
                if (clip == null)
                {
                    Debug.LogWarning("Sound clip not found: Sounds/" + name);
                    continue;
                }
                clips.Add(clip);
            }
            return clips.ToArray();
        }
    }
}
