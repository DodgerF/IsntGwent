using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Cards.Client;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    public class CardPreviewWindow : MonoBehaviour
    {
        public Image image;
        public TextMeshProUGUI description;
        public GameObject descriptionBackground;
        public TextMeshProUGUI power;
        public GameObject preview;
        public GameObject meleeIcon;
        public GameObject rangedIcon;
        [Inject] private CardPreviewService _previewService;  
        
        private void Start()  
        {  
            _previewService.ShowCard  
                .Subscribe(Show)  
                .AddTo(this);  
		  
            _previewService.HideCard  
                .Subscribe(_ => Hide())  
                .AddTo(this);
            Hide(); 
        } 
        
        private void Show(CardInstance card)  
        {  
            preview.SetActive(true);  
            Setup(card);
        }
        
        public void Setup(CardInstance card)
        {
            var spritePath = "Sprites/Cards/" + card.Definition.ImageName;
            image.sprite = Resources.Load<Sprite>(spritePath);

            if (card.Definition is UnitDefinition unit)
            {
                meleeIcon.SetActive(unit.Row == RowType.Melee);
                rangedIcon.SetActive(unit.Row == RowType.Ranged);
                
                power.gameObject.SetActive(true);
                power.text = unit.Power.ToString();
            }
            else
            {
                power.gameObject.SetActive(false);
                meleeIcon.SetActive(false);
                rangedIcon.SetActive(false);
            }
            
            description.text = card.Definition.Description;
            descriptionBackground.SetActive(card.Definition.Description != null);
        }
	
        private void Hide()  
        {  
            preview.SetActive(false);  
        }
    }
}