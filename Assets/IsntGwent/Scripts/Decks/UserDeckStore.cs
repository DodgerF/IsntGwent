using System.IO;
using IsntGwent.Scripts.Decks.Definitions;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Decks
{
    public class UserDeckStore : IInitializable
    {
        private const string FolderName = "Decks";

        public readonly ReactiveCollection<DeckDefinition> Decks = new();

        private string _directory;

        public void Initialize()
        {
            _directory = Path.Combine(Application.persistentDataPath, FolderName);

            try
            {
                Directory.CreateDirectory(_directory);
            }
            catch (IOException e)
            {
                Debug.LogError("Failed to create deck folder: " + e.Message);
                return;
            }

            foreach (var file in Directory.GetFiles(_directory, "*.json"))
            {
                var deck = Read(file);
                if (deck != null) Decks.Add(deck);
            }
        }

        public void Save(DeckDefinition deck)
        {
            if (deck == null || string.IsNullOrEmpty(deck.Id)) return;

            var index = IndexOf(deck.Id);

            if (index < 0)
                Decks.Add(deck);
            else
                Decks[index] = deck;

            Write(deck);
        }

        public void Delete(string deckId)
        {
            var index = IndexOf(deckId);
            if (index < 0) return;

            Decks.RemoveAt(index);

            var path = FilePath(deckId);

            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException e)
            {
                Debug.LogError("Failed to delete deck " + deckId + ": " + e.Message);
            }
        }

        public bool Contains(string deckId)
        {
            return IndexOf(deckId) >= 0;
        }

        private static DeckDefinition Read(string path)
        {
            try
            {
                var deck = JsonConvert.DeserializeObject<DeckDefinition>(File.ReadAllText(path));
                return string.IsNullOrEmpty(deck?.Id) ? null : deck;
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to read deck " + path + ": " + e.Message);
                return null;
            }
        }

        private void Write(DeckDefinition deck)
        {
            if (string.IsNullOrEmpty(_directory)) return;

            try
            {
                File.WriteAllText(FilePath(deck.Id), JsonConvert.SerializeObject(deck, Formatting.Indented));
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to save deck " + deck.Id + ": " + e.Message);
            }
        }

        private string FilePath(string deckId)
        {
            return Path.Combine(_directory, SafeName(deckId) + ".json");
        }

        private static string SafeName(string deckId)
        {
            var chars = deckId.ToCharArray();
            var invalid = Path.GetInvalidFileNameChars();

            for (var i = 0; i < chars.Length; i++)
            {
                if (System.Array.IndexOf(invalid, chars[i]) >= 0) chars[i] = '_';
            }

            return new string(chars);
        }

        private int IndexOf(string deckId)
        {
            if (string.IsNullOrEmpty(deckId)) return -1;

            for (var i = 0; i < Decks.Count; i++)
            {
                if (Decks[i].Id == deckId) return i;
            }

            return -1;
        }
    }
}
