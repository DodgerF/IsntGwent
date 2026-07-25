using System.Collections.Generic;
using DG.Tweening;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class GraveyardView : MonoBehaviour
    {
        public GameObject cardPrefab;

        [Inject] private readonly DiContainer _container;

        private readonly List<CardInstance> _cards = new();
        private GameObject _topCardGo;

        public void AddCard(CardInstance instance, GameObject existingGo = null)
        {
            _cards.Add(instance);
            GameObject cardGo;

            if (existingGo != null)
            {
                cardGo = existingGo;
            }
            else
            {
                var view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab, transform);
                view.Setup(instance);
                cardGo = view.gameObject;
            }

            var previousRow = cardGo.transform.parent != null
                ? cardGo.transform.parent.GetComponent<RowView>()
                : null;

            var cardTransform = cardGo.transform;
            cardTransform.DOKill();

            var start = cardTransform.position;

            cardTransform.SetParent(transform, false);
            cardTransform.localRotation = Quaternion.identity;

            if (Application.isPlaying)
                cardTransform.position = start;

            previousRow?.RefreshLayout();

            cardGo.GetComponent<CardView>()
                ?.SetTargetHighlight(CardView.TargetHighlightState.None);

            var previousTop = _topCardGo;
            _topCardGo = cardGo;
            _topCardGo.SetActive(true);

            if (!Application.isPlaying)
            {
                cardTransform.localPosition = Vector3.zero;
                cardTransform.localScale = Vector3.one;
                HidePrevious(previousTop, cardGo);
                return;
            }

            DOTween.Sequence()
                .Append(cardTransform
                    .DOLocalMove(Vector3.zero, CardAnimConfig.GraveyardFlightDuration)
                    .SetEase(CardAnimConfig.FlightEase))
                .Join(cardTransform.DOScale(Vector3.one, CardAnimConfig.GraveyardFlightDuration))
                .OnComplete(() => HidePrevious(previousTop, cardGo));
        }

        private static void HidePrevious(GameObject previousTop, GameObject current)
        {
            if (previousTop != null && previousTop != current)
                previousTop.SetActive(false);
        }
    }
}
