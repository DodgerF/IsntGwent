using System;
using System.Collections.Generic;
using IsntGwent.Scripts.Cards.UI;

namespace IsntGwent.Scripts.Cards.Client
{
    public class CardViewRegistry
    {
        private readonly Dictionary<Guid, CardView> _views = new();

        public void Register(CardView view) => _views[view.Instance.Id] = view;

        public void Remove(Guid id) => _views.Remove(id);
        
        public CardView Get(Guid id)
        {
            if (!_views.TryGetValue(id, out var view)) return null;

            return view != null ? view : null;
        }

        public CardView Get(string id) => Guid.TryParse(id, out var guid) ? Get(guid) : null;

        public IEnumerable<KeyValuePair<Guid, CardView>> All
        {
            get
            {
                foreach (var kvp in _views)
                {
                    if (kvp.Value != null)
                        yield return kvp;
                }
            }
        }
    }
}
