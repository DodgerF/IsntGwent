using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using IsntGwent.Scripts.Cards;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Server;
using IsntGwent.Scripts.Cards.Server.Effects;
using IsntGwent.Scripts.Decks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace IsntGwent.Scripts.Content
{
    public class ContentService
    {
        public const string CardsFileName = "cards.json";
        public const int PollTimeoutMs = 20000;

        private static readonly JsonSerializerSettings WireSettings = new()
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Include,
            Formatting = Formatting.None,
        };

        private readonly CardDatabase _cards;
        private readonly DeckRulesProvider _rules;
        private readonly CardResolver _resolver;

        private byte[] _cardsBytes;
        private string _cardsSha1;
        private int _builtVersion = -1;

        public ContentService(CardDatabase cards, DeckRulesProvider rules, CardResolver resolver)
        {
            _cards = cards;
            _rules = rules;
            _resolver = resolver;
        }

        public bool IsReady => _cards.OnLoaded.Value && _rules.OnLoaded.Value;

        public int ContentVersion => _rules.Current.ContentVersion;

        public byte[] CardsJson()
        {
            Build();
            return _cardsBytes;
        }

        public string ManifestJson()
        {
            Build();

            var rules = _rules.Current;

            var manifest = new ManifestWire
            {
                ContentVersion = rules.ContentVersion,
                Rules = new RulesWire
                {
                    MinDeckSize = rules.MinDeckSize,
                    MaxCopiesCommon = rules.MaxCopiesCommon,
                    MaxCopiesRare = rules.MaxCopiesRare,
                    MaxCopiesEpic = rules.MaxCopiesEpic,
                    MaxCopiesLegendary = rules.MaxCopiesLegendary,
                },
                Files =
                {
                    new ManifestFileWire
                    {
                        Path = CardsFileName,
                        Sha1 = _cardsSha1,
                        Size = _cardsBytes?.Length ?? 0,
                    }
                },
                Atlas = null,
                PollTimeoutMs = PollTimeoutMs,
            };

            return JsonConvert.SerializeObject(manifest, WireSettings);
        }

        private void Build()
        {
            if (!IsReady) return;
            if (_builtVersion == ContentVersion && _cardsBytes != null) return;

            var payload = new CardsWire { ContentVersion = ContentVersion };

            foreach (var card in _cards.Cards.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
                payload.Cards.Add(ToWire(card));

            var json = JsonConvert.SerializeObject(payload, WireSettings);

            _cardsBytes = Encoding.UTF8.GetBytes(json);
            _cardsSha1 = Sha1(_cardsBytes);
            _builtVersion = ContentVersion;
        }

        private CardWire ToWire(CardDefinition card)
        {
            var wire = new CardWire
            {
                Id = card.Id,
                Name = card.Name,
                ImageName = card.ImageName,
                Type = card.Type,
                Rarity = card.Rarity,
                Power = card is UnitDefinition unit ? unit.Power : 0,
                Description = card.Description ?? string.Empty,
                MaxCopies = _rules.MaxCopiesFor(card),
                IsToken = card.IsToken,
                RequiresTargets = _resolver.NeedsManualTargets(card, out var targetCount),
                RequiresRow = _resolver.NeedsRowChoice(card),
            };

            wire.TargetCount = wire.RequiresTargets ? targetCount : 0;

            var manual = ManualTargeting(card);
            if (manual == null) return wire;

            wire.AllowPartial = manual.AllowPartial;
            wire.TargetIncludeAllies = manual.IncludeAllies;
            wire.TargetIncludeEnemies = manual.IncludeEnemies;
            wire.AimsAfterPlace = manual is AimedTargetingDefinition;

            return wire;
        }

        private static ManualTargetingDefinition ManualTargeting(CardDefinition card)
        {
            foreach (var effect in card.Effects)
            {
                if (effect.Trigger != EffectTrigger.OnPlay) continue;
                if (effect is ManualTargetingDefinition manual) return manual;
            }

            return null;
        }

        private static string Sha1(byte[] bytes)
        {
            using var sha = SHA1.Create();

            var hash = sha.ComputeHash(bytes);
            var builder = new StringBuilder(hash.Length * 2);

            foreach (var b in hash)
                builder.Append(b.ToString("x2"));

            return builder.ToString();
        }
    }
}
