using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace IsntGwent {
    /// <summary>
    /// Выставляет хранимые в нем карты на поле
    /// </summary>
    public class CardList : MonoBehaviour
    {
        [SerializeField] private float _offsetX = 0.1f;

        private List<Card> _cards;

        private void Start()
        {
            //временно в Start
            _cards = GetComponentsInChildren<Card>().ToList();
            //временно в Start
            ArrangeTheCards();
        }

        private void ArrangeTheCards()
        {
            if (_cards.Count == 0) return;
            
            float cardWidth = Card.Width;
            int cardsAmount = _cards.Count;

            int cardsOnEachSide = cardsAmount / 2;
            bool isEvenNumberOfCards = cardsAmount % 2 == 0;

            float offset = _offsetX + cardWidth;
            float offsetFromCenter = (isEvenNumberOfCards ? (offset / 2) : 0);


            for (int i = 0; i < cardsOnEachSide; i++)
            {
                float x = ((i - cardsOnEachSide) * offset) + offsetFromCenter;

                _cards[i].transform.localPosition = new Vector3(x, _cards[i].transform.localPosition.y, _cards[i].transform.localPosition.z);
            }

            //чтобы при нечетном количестве карт, центральная оставалась в центре
            int cardNumberOfCenter = (isEvenNumberOfCards ? 1 : 0);

            for (int i = cardsOnEachSide; i < cardsAmount; i++)
            { 
                float x = (cardNumberOfCenter * offset) - offsetFromCenter;

                _cards[i].transform.localPosition = new Vector3(x, _cards[i].transform.localPosition.y, _cards[i].transform.localPosition.z);

                cardNumberOfCenter++;
            }
        }
    }

}
