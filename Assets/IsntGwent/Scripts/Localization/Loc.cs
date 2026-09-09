using System.Collections.Generic;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using UniRx;
using UnityEngine;

namespace IsntGwent.Scripts.Localization
{
    public static class Loc
    {
        private static LocalizationService _service;

        public static readonly ReactiveProperty<string> Language = new(LocalizationService.BaseCode);

        public static LocalizationService Service => _service;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _service = null;
            Language.Value = LocalizationService.BaseCode;
        }

        public static void Bind(LocalizationService service) => _service = service;

        public static void Push(string code) => Language.Value = code;

        public static string T(string source)
        {
            return _service == null ? source : _service.T(source);
        }

        public static string F(string source, params object[] args)
        {
            var format = T(source);

            return args == null || args.Length == 0 ? format : string.Format(format, args);
        }

        public static string SourceOf(string displayed)
        {
            return _service?.SourceOf(displayed);
        }

        public static string CardName(CardDefinition definition)
        {
            return _service == null ? definition?.Name ?? string.Empty : _service.CardName(definition);
        }

        public static string CardDescription(CardDefinition definition)
        {
            return _service == null ? definition?.Description ?? string.Empty : _service.CardDescription(definition);
        }

        public static List<KeywordEntry> Keywords() => _service?.Keywords();
    }
}
