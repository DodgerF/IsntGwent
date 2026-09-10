using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Runtime;
using Zenject;

namespace IsntGwent.Scripts.Cards.Client
{
    public class CardInstanceRegistry
    {
        [Inject] private readonly CardDatabase _cardDatabase;

        private readonly Dictionary<Guid, CardInstance> _instances = new();

        public CardInstance GetOrCreate(string definitionId, string instanceId)
        {
            if (!Guid.TryParse(instanceId, out var id))
                return null;

            if (_instances.TryGetValue(id, out var existing))
                return existing;

            var instance = CardFactory.Create(_cardDatabase.Get(definitionId));
            instance.SetId(id);
            _instances[id] = instance;

            return instance;
        }

        public CardInstance Get(Guid id) => _instances.TryGetValue(id, out var instance) ? instance : null;

        public CardInstance Get(string id) => Guid.TryParse(id, out var guid) ? Get(guid) : null;

        public void Remove(Guid id) => _instances.Remove(id);

        public void Clear() => _instances.Clear();
    }
}
