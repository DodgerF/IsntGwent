using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class HandPresenter : MonoBehaviour
    {
        public RowView hand;
        public GameObject cardPrefab;
        public RectTransform deckAnchor;
        public RedrawPresenter redraw;

        [Inject] private readonly DiContainer _container;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;

        private void Start()
        {
            _matchState.Hand
                .ObserveAdd()
                .Subscribe(e =>
                {
                    var parent = deckAnchor != null ? deckAnchor : hand.transform;

                    var view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab, parent);
                    view.Setup(e.Value);
                    _registry.Register(view);

                    var target = redraw != null && redraw.IsPhaseActive ? redraw.NextRow() : hand;
                    target.AddCard(view.gameObject);
                })
                .AddTo(this);
        }
    }
}
