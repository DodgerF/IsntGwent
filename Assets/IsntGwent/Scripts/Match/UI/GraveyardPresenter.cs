using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class GraveyardPresenter : MonoBehaviour
    {
        public GraveyardView ownGraveyard;
        public GraveyardView enemyGraveyard;

        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;

        private void Start()
        {
            BindGraveyard(_matchState.OwnGraveyard, ownGraveyard);
            BindGraveyard(_matchState.EnemyGraveyard, enemyGraveyard);
        }

        private void BindGraveyard(ReactiveCollection<CardInstance> collection, GraveyardView graveyard)
        {
            collection
                .ObserveAdd()
                .Subscribe(e =>
                {
                    var view = _registry.Get(e.Value.Id);
                    graveyard.AddCard(e.Value, view != null ? view.gameObject : null);
                })
                .AddTo(this);
        }
    }
}
