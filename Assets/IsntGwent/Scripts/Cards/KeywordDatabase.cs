using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Localization;
using Newtonsoft.Json;
using UniRx;
using IsntGwent.Scripts.Diagnostics;
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

    public class KeywordDatabase : IInitializable, IDisposable
    {
        private const string Paragraph = "\n";

        private readonly CoroutineRunner _runner;
        private readonly List<KeywordEntry> _base = new();
        private readonly List<KeywordEntry> _keywords = new();
        private readonly CompositeDisposable _disposables = new();

        public ReactiveProperty<bool> OnLoaded = new();

        public KeywordDatabase(CoroutineRunner runner) => _runner = runner;

        public void Initialize()
        {
            Loc.Language
                .Subscribe(_ => Rebuild())
                .AddTo(_disposables);

            _runner.StartCoroutine(LoadCoroutine());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        private void Rebuild()
        {
            var localized = Loc.Keywords();

            _keywords.Clear();
            _keywords.AddRange(localized is { Count: > 0 } ? localized : _base);
            _keywords.Sort((a, b) => b.word.Length - a.word.Length);
        }

        private IEnumerator LoadCoroutine()
        {
            yield return StreamingAssetsLoader.LoadAllJson(
                folder: "Keywords",
                onComplete: jsonList =>
                {
                    foreach (var json in jsonList)
                        Parse(json);

                    Rebuild();
                    OnLoaded.Value = true;
                },
                onError: err =>
                {
                    Log.Error(LogTag.Data, err);
                    OnLoaded.Value = true;
                }
            );
        }

        private void Parse(string json)
        {
            json = json.TrimStart('﻿', '​');
            var file = JsonConvert.DeserializeObject<KeywordFile>(json);
            if (file?.keywords != null) _base.AddRange(file.keywords);
        }

        public string Format(string description)
        {
            if (string.IsNullOrEmpty(description) || _keywords.Count == 0)
                return description;

            var builder = new StringBuilder(description.Length);
            var index = 0;
            var sentenceStart = 0;
            var broken = false;

            while (index < description.Length)
            {
                var matched = MatchAt(description, index);
                if (matched == null)
                {
                    var symbol = description[index];
                    builder.Append(symbol);
                    index++;

                    if (symbol == '.')
                    {
                        sentenceStart = builder.Length;
                        broken = false;
                    }

                    continue;
                }

                if (!broken && sentenceStart > 0 && OpensClause(description, index + matched.word.Length))
                {
                    var tail = builder.ToString(sentenceStart, builder.Length - sentenceStart).TrimStart();
                    builder.Length = sentenceStart;
                    builder.Append(Paragraph).Append(tail);
                    broken = true;
                }

                var colored = !string.IsNullOrWhiteSpace(matched.color);

                if (colored) builder.Append("<color=").Append(matched.color).Append('>');
                builder.Append("<b>").Append(matched.word).Append("</b>");
                if (colored) builder.Append("</color>");

                index += matched.word.Length;
            }

            return builder.ToString();
        }

        private static bool OpensClause(string text, int index)
        {
            if (index > 0 && text[index - 1] == ':') return true;

            while (index < text.Length && text[index] == ' ') index++;

            if (index < text.Length && text[index] == '(')
            {
                var close = text.IndexOf(')', index);
                if (close < 0) return false;

                index = close + 1;

                while (index < text.Length && text[index] == ' ') index++;
            }

            return index < text.Length && text[index] == ':';
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
