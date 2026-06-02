using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Services;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    public class CardView : MonoBehaviour,
        IPointerDownHandler,
        IPointerUpHandler 
    {
        public Image cardImage;
        public TextMeshProUGUI powerText;
        public GameObject meleeIcon;
        public GameObject rangedIcon;
        
        private bool _pressed = false;
        private CardDefinition _definition;  
        [Inject] private CardPreviewService _previewService;  
        
        private void OnLongPress()  
        {  
            _previewService.ShowCard.OnNext(_definition);  
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true; 
            Observable.Timer(System.TimeSpan.FromSeconds(0.5f)) 
                .Where(_ => _pressed) 
                .Subscribe(_ =>
                {
                    OnLongPress();
                    _pressed = false;
                }) 
                .AddTo(this); 
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pressed = false;
            _previewService.HideCard.OnNext(Unit.Default);
        }
        
        public void Setup(CardDefinition card)
        {
            _definition = card;
            var spritePath = "Sprites/Cards/" + card.ImageName;
            cardImage.sprite = Resources.Load<Sprite>(spritePath);

            if (card is UnitDefinition unit)
            {
                powerText.text = unit.Power.ToString();

                meleeIcon.SetActive(unit.Row == RowType.Melee);
                rangedIcon.SetActive(unit.Row == RowType.Ranged);
            }
            else
            {
                powerText.gameObject.SetActive(false);

                meleeIcon.SetActive(false);
                rangedIcon.SetActive(false);
            }
        }
    }
}