using System.Collections;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Decks.Definitions;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Decks
{
    public class DeckRulesProvider : IInitializable
    {
        private readonly CoroutineRunner _runner;

        public ReactiveProperty<bool> OnLoaded = new();
        public DeckRules Current { get; private set; } = new();

        public DeckRulesProvider(CoroutineRunner runner)
        {
            _runner = runner;
        }

        public void Initialize()
        {
            _runner.StartCoroutine(LoadCoroutine());
        }

        public int MaxCopiesFor(CardDefinition card)
        {
            if (card == null) return 0;
            if (card.MaxCopies > 0) return card.MaxCopies;

            return card.Rarity switch
            {
                CardRarity.Rare => Current.MaxCopiesRare,
                CardRarity.Epic => Current.MaxCopiesEpic,
                CardRarity.Legendary => Current.MaxCopiesLegendary,
                CardRarity.Token => 0,
                _ => Current.MaxCopiesCommon
            };
        }

        private IEnumerator LoadCoroutine()
        {
            yield return StreamingAssetsLoader.LoadAllJson(
                folder: "Rules",
                onComplete: jsonList =>
                {
                    foreach (var json in jsonList)
                    {
                        var rules = JsonConvert.DeserializeObject<DeckRules>(json);
                        if (rules != null) Current = rules;
                    }
                    OnLoaded.Value = true;
                },
                onError: err =>
                {
                    Debug.LogError(err);
                    OnLoaded.Value = true;
                }
            );
        }
    }
}
