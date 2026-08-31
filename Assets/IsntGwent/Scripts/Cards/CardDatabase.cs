using System;
using System.Collections;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Core;
using IsntGwent.Scripts.Cards.Server.Effects;
using Newtonsoft.Json;
using UniRx;
using IsntGwent.Scripts.Diagnostics;
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
                onError: err => Log.Error(LogTag.Data, err)
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
                    "AimedTargeting" => effectJson.ToObject<AimedTargetingDefinition>(),
                    "WeakestTargeting" => effectJson.ToObject<WeakestTargetingDefinition>(),
                    "StrongestTargeting" => effectJson.ToObject<StrongestTargetingDefinition>(),
                    "NeighborTargeting" => effectJson.ToObject<NeighborTargetingDefinition>(),
                    "SlotTargeting" => effectJson.ToObject<SlotTargetingDefinition>(),
                    "LineTargeting" => effectJson.ToObject<LineTargetingDefinition>(),
                    "RowTargeting" => effectJson.ToObject<RowTargetingDefinition>(),
                    "EventTargeting" => effectJson.ToObject<EventTargetingDefinition>(),
                    "AreaTargeting" => effectJson.ToObject<RowTargetingDefinition>(),
                    "DealDamage" => effectJson.ToObject<DealDamageEffectDefinition>(),
                    "BuffPower" => effectJson.ToObject<BuffPowerDefinition>(),
                    "SetPower" => effectJson.ToObject<SetPowerDefinition>(),
                    "Devour" => effectJson.ToObject<DevourDefinition>(),
                    "Destroy" => effectJson.ToObject<DestroyEffectDefinition>(),
                    "SummonFromDeck" => effectJson.ToObject<SummonFromDeckDefinition>(),
                    "SummonToken" => effectJson.ToObject<SummonTokenDefinition>(),
                    "Move" => effectJson.ToObject<MoveEffectDefinition>(),
                    "GainArmor" => effectJson.ToObject<GainArmorDefinition>(),
                    "Heal" => effectJson.ToObject<HealDefinition>(),
                    "PurgeGraveyard" => effectJson.ToObject<PurgeGraveyardDefinition>(),
                    "CastFromDeck" => effectJson.ToObject<CastFromDeckDefinition>(),
                    "ApplyWeather" => effectJson.ToObject<ApplyWeatherDefinition>(),
                    _ => throw new Exception("Unknown Effect: " + effectType)
                };
                card.Effects.Add(effect);
            }

            Cards.Add(card.Id, card);
        }
    }
}