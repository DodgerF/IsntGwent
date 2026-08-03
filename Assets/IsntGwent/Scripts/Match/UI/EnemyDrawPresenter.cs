using DG.Tweening;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class EnemyDrawPresenter : MonoBehaviour
    {
        public GameObject cardBackPrefab;
        public RectTransform enemyDeckAnchor;
        public RectTransform enemyHandAnchor;

        [Inject] private readonly MatchState _matchState;

        private void Start()
        {
            _matchState.EnemyCardDrawn
                .Subscribe(_ => Play())
                .AddTo(this);
        }

        private void Play()
        {
            if (cardBackPrefab == null || enemyDeckAnchor == null || enemyHandAnchor == null) return;

            var back = Instantiate(cardBackPrefab, transform);
            back.transform.position = enemyDeckAnchor.position;

            back.transform
                .DOMove(enemyHandAnchor.position, CardAnimConfig.EnemyDrawFlightDuration)
                .SetEase(CardAnimConfig.FlightEase)
                .OnComplete(() => Destroy(back));
        }
    }
}
