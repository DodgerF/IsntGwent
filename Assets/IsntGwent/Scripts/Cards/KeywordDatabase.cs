using System.Collections;
using System.Collections.Generic;
using System.Text;
using IsntGwent.Scripts.Core;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Cards
{
    public class KeywordEntry
    {
        public string word;
        public string category;
        public string color;
        public string title;
        public string description;

        public string Title() => string.IsNullOrWhiteSpace(title) ? word.TrimEnd(':') : title;
    }

    public class KeywordFile
    {
        public List<KeywordEntry> keywords;
    }

    public class KeywordDatabase : IInitializable
    {
        private readonly CoroutineRunner _runner;
        private readonly List<KeywordEntry> _keywords = new();

        public ReactiveProperty<bool> OnLoaded = new();

        public KeywordDatabase(CoroutineRunner runner) => _runner = runner;

        public void Initialize()
        {
            _runner.StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            yield return StreamingAssetsLoader.LoadAllJson(
                folder: "Keywords",
                onComplete: jsonList =>
                {
                    foreach (var json in jsonList)
                        Parse(json);

                    _keywords.Sort((a, b) => b.word.Length - a.word.Length);
                    OnLoaded.Value = true;
                },
                onError: err =>
                {
                    Debug.LogError(err);
                    OnLoaded.Value = true;
                }
            );
        }

        private void Parse(string json)
        {
            json = json.TrimStart('﻿', '​');
            var file = JsonConvert.DeserializeObject<KeywordFile>(json);
            if (file?.keywords != null) _keywords.AddRange(file.keywords);
        }

        public string Format(string description)
        {
            if (string.IsNullOrEmpty(description) || _keywords.Count == 0)
                return description;

            var builder = new StringBuilder(description.Length);
            var index = 0;

            while (index < description.Length)
            {
                var matched = MatchAt(description, index);
                if (matched == null)
                {
                    builder.Append(description[index]);
                    index++;
                    continue;
                }

                builder.Append("<b>").Append(matched.word).Append("</b>");
                index += matched.word.Length;
            }

            return builder.ToString();
        }

        public List<KeywordEntry> Used(string description)
        {
            var used = new List<KeywordEntry>();

            if (string.IsNullOrEmpty(description) || _keywords.Count == 0)
                return used;

            var index = 0;

            while (index < description.Length)
            {
                var matched = MatchAt(description, index);

                if (matched == null)
                {
                    index++;
                    continue;
                }

                index += matched.word.Length;

                if (string.IsNullOrWhiteSpace(matched.description)) continue;
                if (used.Exists(entry => entry.Title() == matched.Title())) continue;

                used.Add(matched);
            }

            return used;
        }

        private KeywordEntry MatchAt(string text, int index)
        {
            foreach (var keyword in _keywords)
            {
                var word = keyword.word;
                if (index + word.Length > text.Length) continue;
                if (string.CompareOrdinal(text, index, word, 0, word.Length) != 0) continue;

                if (index > 0 && char.IsLetter(text[index - 1]) && char.IsLetter(word[0]))
                    continue;

                var end = index + word.Length;
                if (end < text.Length && char.IsLetter(text[end]) && char.IsLetter(word[word.Length - 1]))
                    continue;

                return keyword;
            }

            return null;
        }
    }
}
