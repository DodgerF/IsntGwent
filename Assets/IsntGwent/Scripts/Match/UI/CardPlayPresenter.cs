using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class CardPlayPresenter : MonoBehaviour
    {
        public GameObject cardPrefab;

        [FormerlySerializedAs("previewAnchor")]
        public RectTransform playAnchor;

        [Inject] private readonly DiContainer _container;
        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;

        private void Start()
        {
            _matchState.CardStaged
                .Subscribe(Stage)
                .AddTo(this);
        }

        private void Stage(CardInstance card)
        {
            var view = _registry.Get(card.Id);

            if (view == null)
            {
                if (cardPrefab == null) return;

                view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab);
                view.Setup(card);
                _registry.Register(view);
            }

            var previousRow = view.transform.parent != null
                ? view.transform.parent.GetComponent<RowView>()
                : null;

            if (previousRow != null)
                previousRow.DetachCard(view.gameObject);

            view.transform.SetParent(transform, false);
            view.transform.position = playAnchor != null
                ? playAnchor.position
                : transform.position;

            view.gameObject.SetActive(true);
        }
    }
}
