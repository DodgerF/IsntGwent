using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Effects;
using Newtonsoft.Json;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Cards
{
    public class CardJsonBase
    {
        public string Type;
    }
    public class CardDatabase : IInitializable
    {
        public readonly Dictionary<string, CardDefinition> Cards = new();
        private readonly CoroutineRunner  _runner;
        public ReactiveProperty<bool> OnLoaded = new();
        public CardDatabase(CoroutineRunner  runner) => _runner = runner;

        public CardDefinition Get(string id)
        {
            return Cards[id];
        }
        public void Initialize()
        {
            _runner.StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            yield return StreamingAssetsLoader.LoadAllJson(
                folder: "Cards",
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
            json = json.TrimStart('\uFEFF', '\u200B');
            var baseData = JsonConvert.DeserializeObject<CardJsonBase>(json);

            CardDefinition card = baseData.Type switch
            {
                "Spell" => JsonConvert.DeserializeObject<SpellDefinition>(json),
                "Unit"  => JsonConvert.DeserializeObject<UnitDefinition>(json),
                _       => throw new Exception("Unknown Card type: " + baseData.Type)
            };

            foreach (var effectJson in card.RawEffects)
            {
                string effectType = effectJson["effect"]?.ToString();
                if (effectType == null) continue;

                EffectDefinition effect = effectType switch
                {
                    "ManualTargeting" => effectJson.ToObject<ManualTargetingDefinition>(),
                    "WeakestTargeting" => effectJson.ToObject<WeakestTargetingDefinition>(),
                    "DealDamage" => effectJson.ToObject<DealDamageEffectDefinition>(),
                    _ => throw new Exception("Unknown Effect: " + effectType)
                };
                card.Effects.Add(effect);
            }

            Cards.Add(card.Id, card);
        }
    }
}