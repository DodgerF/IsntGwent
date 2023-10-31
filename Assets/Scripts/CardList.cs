using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace IsntGwent {
    //¬ыставл€ет хранимые в нем карты на поле
    [RequireComponent(typeof(RectTransform))]
    public class CardList : MonoBehaviour
    {
        [SerializeField] private float _offsetX;

        public List<Card> _cards;

        private void Awake()
        {
            //временно в Awake
            _cards = GetComponentsInChildren<Card>().ToList();
            
            SortCards();
        }

        private void SortCards()
        {
            float cardWidth = _cards[0].GetComponent<RectTransform>().rect.width;
            int cardsAmount = _cards.Count;

            int cardsOnEachSide = cardsAmount / 2;
            bool isEvenNumberOfCards = cardsAmount % 2 == 0;

            float offset = cardWidth;
            
            for (int i = 0; i < cardsOnEachSide; i++)
            {
                float x = -((i - cardsOnEachSide) * offset) + (isEvenNumberOfCards ? (offset / 2) : 0);
                Vector3 vec = new Vector3(x, _cards[i].transform.position.y, _cards[i].transform.position.z);
                Debug.Log(vec);
                _cards[i].transform.position = vec;
                Debug.Log(_cards[i].transform.position);
            }

            for (int i = cardsOnEachSide + 1; i < cardsAmount - 1; i++)
            {
                float x = (i * offset) - (isEvenNumberOfCards ? (offset / 2) : 0);

                _cards[i].transform.position = new Vector3(x, _cards[i].transform.position.y, _cards[i].transform.position.z);
            }

        }
    }

}
