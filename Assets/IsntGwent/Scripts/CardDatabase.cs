using System;
using System.Collections.Generic;
using System.IO;
using IsntGwent.Scripts.Cards.Definitions;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts
{
    public class CardJsonBase
    {
        public string Type;
    }
    public class CardDatabase : IInitializable
    {
        private readonly Dictionary<string, CardDefinition> _cards = new();

        public CardDefinition Get(string id)
        {
            return _cards[id];
        }

        private void LoadAll(string directory)
        {
            var files = Directory.GetFiles(directory, "*.json");

            foreach (var file in files)
            {
                var json = File.ReadAllText(file);
                var baseData = JsonConvert.DeserializeObject<CardJsonBase>(json);
                
                CardDefinition card = baseData.Type switch
                {
                    "Spell" =>
                        JsonConvert.DeserializeObject<SpellDefinition>(json),
                    "Unit" =>
                        JsonConvert.DeserializeObject<UnitDefinition>(json),
                    
                    _ => throw new Exception("Unknown Card type: " + baseData.Type)
                };
                
                foreach (var effectJson in card.RawEffects)
                {
                    string effectType = effectJson["effect"]?.ToString();
                    if (effectType == null)  continue;
                    
                    var effect = effectType switch
                    {
                        "DealDamage" =>
                            effectJson.ToObject<DealDamageEffectDefinition>(),
                        
                        _ => throw new Exception("Unknown Effect: " + effectType)
                    };
                    card.Effects.Add(effect); 
                }
                
                _cards.Add(card.Id, card);
            }
        }

        public void Initialize()
        {
            LoadAll(Path.Combine(Application.streamingAssetsPath, "Cards"));
        }
    }
}