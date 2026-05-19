using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts
{
    public class DeckDatabase : IInitializable
    {
        private readonly Dictionary<string, DeckDefinition> _decks = new();

        public DeckDefinition Get(string id)
        {
            return _decks[id];
        }

        public IReadOnlyCollection<DeckDefinition> GetAll()
        {
            return _decks.Values;
        }

        private void LoadAll(string directory)
        {
            var files = Directory.GetFiles(directory, "*.json");
            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var deck = JsonConvert.DeserializeObject<DeckDefinition>(json);
                
                _decks.Add(deck.Id, deck);
            }
        }

        public void Initialize()
        {
            LoadAll(Path.Combine(Application.streamingAssetsPath, "Decks"));
        }
    }
}