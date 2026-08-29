using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class PendingPlayPresenter : MonoBehaviour
    {
        public PendingPlayLaneView lane;
        public GameObject cardPrefab;
        public RectTransform ownDeckAnchor;
        public RectTransform enemyDeckAnchor;

        [Inject] private readonly DiContainer _container;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;

        private void Start()
        {
            _matchState.PendingPlays
                .ObserveAdd()
                .Subscribe(e => Show(e.Value))
                .AddTo(this);

            _matchState.PendingPlays
                .ObserveRemove()
                .Subscribe(e => Release(e.Value))
                .AddTo(this);
        }

        private void Show(CardInstance card)
        {
            if (lane == null) return;

            var view = _registry.Get(card.Id);

            if (view == null)
            {
                if (cardPrefab == null) return;

                var anchor = _matchState.IsPendingMine.Value ? ownDeckAnchor : enemyDeckAnchor;
                var parent = anchor != null ? anchor.transform : lane.transform;

                view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab, parent);
                view.Setup(card);
                _registry.Register(view);
            }

            view.mode = CardMode.InHand;
            view.hoverScale = true;
            view.gameObject.SetActive(true);
            lane.AddCard(view.gameObject);
        }

        private void Release(CardInstance card)
        {
            var view = _registry.Get(card.Id);
            if (view == null) return;
            if (lane == null || !lane.Contains(view.gameObject)) return;

            _registry.Remove(card.Id);
            lane.RemoveCard(view.gameObject);
        }
    }
}
