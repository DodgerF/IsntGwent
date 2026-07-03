using System.Collections.Generic;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.UI;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match
{
    public class GraveyardView : MonoBehaviour
    {
        public GameObject cardPrefab;

        [Inject] private readonly DiContainer _container;

        private readonly List<CardInstance> _cards = new();
        private GameObject _topCardGo;

        private void Awake()
        {
            if (_topCardGo != null)
                _topCardGo.SetActive(false);
        }

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

            cardGo.transform.SetParent(transform, false);
            cardGo.transform.localPosition = Vector3.zero;

            if (_topCardGo != null)
                _topCardGo.SetActive(false);

            _topCardGo = cardGo;
            _topCardGo.SetActive(true);
        }
        
        private void RefreshTop(GameObject newGo)
        {
            if (_topCardGo != null)
                _topCardGo.SetActive(false);

            if (_cards.Count == 0) return;

            if (newGo != null)
            {
                _topCardGo = newGo;
            }
            else
            {
                var view = _container.InstantiatePrefabForComponent<CardView>(cardPrefab, transform);
                view.Setup(_cards[^1]);
                _topCardGo = view.gameObject;
            }

            _topCardGo.SetActive(true);
        }
    }
}