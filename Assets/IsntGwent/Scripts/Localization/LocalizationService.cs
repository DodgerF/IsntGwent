using System.Collections;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Diagnostics;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Localization
{
    public class LocalizationService : IInitializable
    {
        public const string BaseCode = "en";

        private const string PrefKey = "lang";
        private const int MaxChain = 8;

        private readonly CoroutineRunner _runner;
        private readonly Dictionary<string, LocaleFile> _byCode = new();
        private readonly List<LocaleFile> _ordered = new();
        private readonly Dictionary<string, string> _sources = new();

        public readonly ReactiveProperty<string> Current = new(BaseCode);
        public readonly ReactiveProperty<bool> OnLoaded = new();

        public IReadOnlyList<LocaleFile> Locales => _ordered;

        public LocalizationService(CoroutineRunner runner) => _runner = runner;

        public void Initialize()
        {
            Loc.Bind(this);
            _runner.StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            yield return StreamingAssetsLoader.LoadAllJson(
                folder: "Locales",
                onComplete: jsonList =>
                {
                    foreach (var json in jsonList)
                        Parse(json);

                    Finish();
                },
                onError: err =>
                {
                    Log.Error(LogTag.Data, err);
                    Finish();
                });
        }

        private void Parse(string json)
        {
            json = json.TrimStart('﻿', '​');

            var locale = JsonConvert.DeserializeObject<LocaleFile>(json);
            if (locale == null || string.IsNullOrWhiteSpace(locale.code)) return;

            locale.strings ??= new Dictionary<string, string>();
            _byCode[locale.code] = locale;
        }

        private void Finish()
        {
            _ordered.Clear();
            _ordered.AddRange(_byCode.Values);
            _ordered.Sort((a, b) => a.order != b.order
                ? a.order.CompareTo(b.order)
                : string.CompareOrdinal(a.code, b.code));

            BuildSourceIndex();

            Current.Value = Restore();
            Current.Subscribe(code => PlayerPrefs.SetString(PrefKey, code));
            Current.Subscribe(Loc.Push);

            OnLoaded.Value = true;
        }

        private void BuildSourceIndex()
        {
            _sources.Clear();

            foreach (var locale in _ordered)
            {
                var seen = new HashSet<string>();
                var node = locale;

                for (var step = 0; node != null && step < MaxChain; step++)
                {
                    foreach (var pair in node.strings)
                    {
                        if (!seen.Add(pair.Key)) continue;

                        var value = pair.Value;
                        if (string.IsNullOrEmpty(value) || value == pair.Key) continue;

                        _sources[value] = pair.Key;
                    }

                    node = Parent(node);
                }
            }
        }

        private LocaleFile Parent(LocaleFile locale)
        {
            if (locale == null || string.IsNullOrEmpty(locale.inherits)) return null;

            return _byCode.TryGetValue(locale.inherits, out var parent) ? parent : null;
        }

        private string Restore()
        {
            var saved = PlayerPrefs.GetString(PrefKey, string.Empty);

            return !string.IsNullOrEmpty(saved) && _byCode.ContainsKey(saved) ? saved : BaseCode;
        }

        public LocaleFile Active()
        {
            return _byCode.TryGetValue(Current.Value, out var locale) ? locale : null;
        }

        public void Select(string code)
        {
            if (string.IsNullOrEmpty(code) || !_byCode.ContainsKey(code)) return;

            Current.Value = code;
        }

        public void Next()
        {
            if (_ordered.Count == 0) return;

            var index = _ordered.FindIndex(locale => locale.code == Current.Value);
            Select(_ordered[(index + 1) % _ordered.Count].code);
        }

        public string T(string source)
        {
            if (string.IsNullOrEmpty(source)) return source;

            Split(source, out var head, out var core, out var tail);
            if (core.Length == 0) return source;

            var node = Active();

            for (var step = 0; node != null && step < MaxChain; step++)
            {
                if (node.strings.TryGetValue(core, out var value) && !string.IsNullOrEmpty(value))
                    return head + value + tail;

                node = Parent(node);
            }

            return source;
        }

        public string SourceOf(string displayed)
        {
            if (string.IsNullOrEmpty(displayed)) return null;

            Split(displayed, out var head, out var core, out var tail);
            if (core.Length == 0) return null;

            return _sources.TryGetValue(core, out var source) ? head + source + tail : null;
        }

        private static void Split(string text, out string head, out string core, out string tail)
        {
            var start = 0;
            var end = text.Length;

            while (start < end && char.IsWhiteSpace(text[start])) start++;
            while (end > start && char.IsWhiteSpace(text[end - 1])) end--;

            head = text.Substring(0, start);
            core = text.Substring(start, end - start);
            tail = text.Substring(end);
        }

        public string CardName(CardDefinition definition)
        {
            if (definition == null) return string.Empty;

            var text = CardText(definition.Id);

            return string.IsNullOrWhiteSpace(text?.name)
                ? definition.Name
                : text.name;
        }

        public string CardDescription(CardDefinition definition)
        {
            if (definition == null) return string.Empty;

            var text = CardText(definition.Id);

            return string.IsNullOrWhiteSpace(text?.description)
                ? definition.Description
                : text.description;
        }

        private LocaleCardText CardText(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            var node = Active();

            for (var step = 0; node != null && step < MaxChain; step++)
            {
                if (node.cards != null && node.cards.TryGetValue(id, out var text)) return text;

                node = Parent(node);
            }

            return null;
        }

        public List<KeywordEntry> Keywords()
        {
            var node = Active();

            for (var step = 0; node != null && step < MaxChain; step++)
            {
                if (node.keywords is { Count: > 0 }) return node.keywords;

                node = Parent(node);
            }

            return null;
        }
    }
}
