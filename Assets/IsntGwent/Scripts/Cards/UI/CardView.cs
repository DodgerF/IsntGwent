using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Cards.UI
{
    public enum CardMode { InHand, OnBoard }
    
    public class CardView : MonoBehaviour
    {
        public Image cardImage;
        public TextMeshProUGUI powerText;
        public GameObject meleeIcon;
        public GameObject rangedIcon;
        
        public CardInstance Instance { get; private set; }
        
        public CardMode mode = CardMode.InHand;
        
        
        public void Setup(CardInstance instance)
        {
            Instance = instance;
            var cardDefinition = instance.Definition;
            
            var spritePath = "Sprites/Cards/" + cardDefinition.ImageName;
            cardImage.sprite = Resources.Load<Sprite>(spritePath);

            if (cardDefinition is UnitDefinition unit)
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
        public void SetSelected(bool selected)
        {
            gameObject.SetActive(!selected);
        }
    }
}