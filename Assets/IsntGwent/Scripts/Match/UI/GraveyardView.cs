using System.Collections.Generic;
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

            cardGo.transform.SetParent(transform, false);
            cardGo.transform.localPosition = Vector3.zero;
            cardGo.transform.localRotation = Quaternion.identity;
            cardGo.transform.localScale = Vector3.one;

            previousRow?.RefreshLayout();

            cardGo.GetComponent<CardView>()
                ?.SetTargetHighlight(CardView.TargetHighlightState.None);

            if (_topCardGo != null)
                _topCardGo.SetActive(false);

            _topCardGo = cardGo;
            _topCardGo.SetActive(true);
        }
    }
}