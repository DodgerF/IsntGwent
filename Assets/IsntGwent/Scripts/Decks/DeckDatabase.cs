using System.Collections;
using System.Collections.Generic;
using IsntGwent.Scripts.Decks.Definitions;
using IsntGwent.Scripts.Core;
using Newtonsoft.Json;
using UniRx;
using IsntGwent.Scripts.Diagnostics;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Decks
{
    public class DeckDatabase : IInitializable
    {
        private readonly Dictionary<string, DeckDefinition> _decks = new();
        private readonly CoroutineRunner  _runner;
        public ReactiveProperty<bool> OnLoaded = new();

        public DeckDatabase(CoroutineRunner runner)
        {
            _runner = runner;
        }

        public void Initialize()
        {
            _runner.StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            yield return StreamingAssetsLoader.LoadAllJson(
                folder: "Decks",
                onComplete: jsonList =>
                {
                    foreach (var json in jsonList)
                    {
                        var deck = JsonConvert.DeserializeObject<DeckDefinition>(json);
                        _decks.Add(deck.Id, deck);
                    }
                    OnLoaded.Value = true;
                },
                onError: err => Log.Error(LogTag.Data, err)
            );
        }

        public DeckDefinition Get(string id)
        {
            return _decks[id];
        }

        public bool Contains(string id)
        {
            return !string.IsNullOrEmpty(id) && _decks.ContainsKey(id);
        }

        public IReadOnlyCollection<DeckDefinition> GetAll()
        {
            return _decks.Values;
        }
    }
}