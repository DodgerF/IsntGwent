using System.Collections.Generic;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class EnemyHandPresenter : MonoBehaviour
    {
        public CardLaneView hand;
        public GameObject cardBackPrefab;
        public RectTransform enemyDeckAnchor;

        [Inject] private readonly MatchState _matchState;

        private readonly List<GameObject> _backs = new();

        private void Start()
        {
            _matchState.EnemyCardAmount
                .Subscribe(Sync)
                .AddTo(this);
        }

        private void Sync(int amount)
        {
            if (hand == null || cardBackPrefab == null) return;

            _backs.RemoveAll(back => back == null);

            while (_backs.Count > amount)
            {
                var back = _backs[^1];
                _backs.RemoveAt(_backs.Count - 1);
                hand.RemoveCard(back);
            }

            while (_backs.Count < amount)
                _backs.Add(Spawn());
        }

        private GameObject Spawn()
        {
            var parent = enemyDeckAnchor != null ? enemyDeckAnchor : hand.transform;
            var back = Instantiate(cardBackPrefab, parent, false);

            if (back.TryGetComponent<Image>(out var image))
                image.raycastTarget = false;

            hand.AddCard(back);

            return back;
        }
    }
}
