using System;
using System.Collections.Generic;
using System.IO;
using IsntGwent.Scripts.Cards.Definitions;
using Newtonsoft.Json;
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

        public CardDefinition Get(string id)
        {
            return Cards[id];
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
                
                Cards.Add(card.Id, card);
            }
        }

        public void Initialize()
        {
            LoadAll(Path.Combine(Application.streamingAssetsPath, "Cards"));
        }
    }
}