using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Decks.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    public class WeatherBadgeView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        private const string CaptionPrefix = "Weather:";

        [InjectOptional] private readonly CardDatabase _cards;
        [InjectOptional] private readonly CardTooltipView _tooltip;
        [InjectOptional] private readonly AudioService _audio;

        [SerializeField] private TextMeshProUGUI caption;

        private CardDefinition _definition;
        private bool _showing;

        public void Show(string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
            {
                _definition = null;
                HideTooltip();
                gameObject.SetActive(false);
                return;
            }

            _definition = _cards?.Get(cardId);

            if (caption != null)
                caption.text = _definition != null ? CaptionPrefix + "\n" + _definition.Name : CaptionPrefix;

            gameObject.SetActive(true);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_showing)
                HideTooltip();
            else
                ShowTooltip();
        }

        private void ShowTooltip()
        {
            if (_tooltip == null || _definition == null) return;

            _tooltip.Show(_definition, (RectTransform)transform);
            _showing = true;
            _audio?.Play("ui_hover");
        }

        private void HideTooltip()
        {
            if (!_showing) return;

            _tooltip?.Hide();
            _showing = false;
        }

        private void OnDisable()
        {
            HideTooltip();
        }
    }
}
