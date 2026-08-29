using System.Collections.Generic;
using DG.Tweening;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class GraveyardView : MonoBehaviour
    {
        public GameObject cardPrefab;
        public float cardScale = 1f;

        [Inject] private readonly DiContainer _container;
        [Inject] private readonly CardViewRegistry _views;

        private readonly List<CardInstance> _cards = new();
        private readonly Dictionary<CardInstance, GameObject> _cardViews = new();
        private GameObject _topCardGo;

        public void AddCard(CardInstance instance, GameObject existingGo = null)
        {
            if (instance != null && _cards.Contains(instance)) return;

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

            if (instance != null)
                _cardViews[instance] = cardGo;

            var cardView = cardGo.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.mode = CardMode.InGraveyard;
                cardView.SetTargetHighlight(CardView.TargetHighlightState.None);
                cardView.Group().blocksRaycasts = false;
            }

            var previousRow = cardGo.transform.parent != null
                ? cardGo.transform.parent.GetComponent<CardLaneView>()
                : null;

            var cardTransform = cardGo.transform;
            cardTransform.DOKill();

            var start = cardTransform.position;

            cardTransform.SetParent(transform, false);
            cardTransform.localRotation = Quaternion.identity;

            if (Application.isPlaying)
                cardTransform.position = start;

            previousRow?.RefreshLayout();

            var previousTop = _topCardGo;
            _topCardGo = cardGo;
            _topCardGo.SetActive(true);

            if (!Application.isPlaying)
            {
                cardTransform.localPosition = Vector3.zero;
                SetScale(cardView, cardTransform, 0f);
                HidePrevious(previousTop, cardGo);
                return;
            }

            SetScale(cardView, cardTransform, CardAnimConfig.GraveyardFlightDuration);

            cardTransform
                .DOLocalMove(Vector3.zero, CardAnimConfig.GraveyardFlightDuration)
                .SetEase(CardAnimConfig.FlightEase)
                .OnComplete(() => HidePrevious(previousTop, cardGo));
        }

        public void RemoveCard(CardInstance instance)
        {
            if (instance == null) return;
            if (!_cardViews.TryGetValue(instance, out var cardGo)) return;

            _cardViews.Remove(instance);
            _cards.Remove(instance);

            var view = cardGo != null ? cardGo.GetComponent<CardView>() : null;

            if (view != null && _views.Get(instance.Id) == view)
                _views.Remove(instance.Id);

            if (cardGo == _topCardGo)
                _topCardGo = null;

            RevealTop();
            Dissolve(cardGo);
        }

        private void RevealTop()
        {
            if (_topCardGo != null) return;

            for (var i = _cards.Count - 1; i >= 0; i--)
            {
                if (!_cardViews.TryGetValue(_cards[i], out var go) || go == null) continue;

                _topCardGo = go;
                _topCardGo.SetActive(true);
                return;
            }
        }

        private static void Dissolve(GameObject cardGo)
        {
            if (cardGo == null) return;

            cardGo.transform.DOKill();

            if (!Application.isPlaying)
            {
                DestroyImmediate(cardGo);
                return;
            }

            if (!cardGo.activeSelf)
            {
                Destroy(cardGo);
                return;
            }

            var view = cardGo.GetComponent<CardView>();
            var group = view != null ? view.Group() : cardGo.GetComponent<CanvasGroup>();

            if (group == null)
                group = cardGo.AddComponent<CanvasGroup>();

            group.DOKill();
            group
                .DOFade(0f, CardAnimConfig.GraveyardFlightDuration)
                .SetEase(CardAnimConfig.FlightEase)
                .OnComplete(() => Destroy(cardGo));
        }

        private void SetScale(CardView view, Transform cardTransform, float duration)
        {
            if (view != null)
                view.SetBaseScale(cardScale, duration, CardAnimConfig.FlightEase);
            else
                cardTransform.localScale = Vector3.one * cardScale;
        }

        private static void HidePrevious(GameObject previousTop, GameObject current)
        {
            if (previousTop != null && previousTop != current)
                previousTop.SetActive(false);
        }
    }
}
