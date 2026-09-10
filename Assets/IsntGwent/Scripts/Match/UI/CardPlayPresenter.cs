using DG.Tweening;
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
        private const float EnemyHandScale = 0.7f;

        public GameObject cardPrefab;

        [FormerlySerializedAs("previewAnchor")]
        public RectTransform playAnchor;

        public RectTransform enemyHandAnchor;

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
            var fromEnemyHand = false;

            if (view == null)
            {
                if (cardPrefab == null) return;

                view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab);
                view.Setup(card);
                _registry.Register(view);

                fromEnemyHand = enemyHandAnchor != null && Application.isPlaying;
            }

            var previousRow = view.transform.parent != null
                ? view.transform.parent.GetComponent<CardLaneView>()
                : null;

            if (previousRow is BoardRowView)
                return;

            if (previousRow != null)
                previousRow.DetachCard(view.gameObject);

            var staged = view.transform;

            staged.DOKill();
            staged.SetParent(transform, false);
            staged.localRotation = Quaternion.identity;

            var target = playAnchor != null ? playAnchor.position : transform.position;

            view.gameObject.SetActive(true);

            if (!fromEnemyHand)
            {
                staged.position = target;
                view.SetBaseScale(CardAnimConfig.PlayStageScale);
                return;
            }

            staged.position = enemyHandAnchor.position;

            view.SetBaseScale(EnemyHandScale);
            view.SetBaseScale(
                CardAnimConfig.PlayStageScale,
                CardAnimConfig.EnemyStageFlightDuration,
                CardAnimConfig.FlightGrowEase);

            staged
                .DOMove(target, CardAnimConfig.EnemyStageFlightDuration)
                .SetEase(CardAnimConfig.FlightEase);
        }
    }
}
