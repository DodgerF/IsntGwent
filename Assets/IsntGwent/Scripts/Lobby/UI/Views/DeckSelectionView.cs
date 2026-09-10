using IsntGwent.Scripts.Localization;
using System;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Decks.Definitions;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Lobby.UI.Views
{
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(Image))]
    public class DeckSelectionView : MonoBehaviour
    {
        private const string OutlineName = "SelectedAura";
        private const float OutlineWidth = 34f;

        private static readonly Color OutlineHalftone = new(0.89f, 0.72f, 0.42f, 0.75f);
        private static readonly Color OutlineInk = new(0.97f, 0.93f, 0.81f, 0.95f);

        private Image _image;
        private Button _button;
        private HandAuraGraphic _outline;

        private readonly RectTransform[] _outlineTargets = new RectTransform[1];

        public TextMeshProUGUI deckName;
        public GameObject incompletePanel;
        public TextMeshProUGUI incompleteText;

        public DeckDefinition Deck { get; private set; }

        public IObservable<Unit> Clicked => _button.OnClickAsObservable();

        private void Awake()
        {
            _image = GetComponent<Image>();
            _button = GetComponent<Button>();

            _outlineTargets[0] = (RectTransform)transform;
            _outline = CreateOutline();
            _outline.gameObject.SetActive(false);
        }

        public void Setup(Sprite deckSprite, DeckDefinition deck)
        {
            _image.sprite = deckSprite;
            Deck = deck;
            deckName.text = Loc.T(deck.Name);
        }

        public void SetInteractable(bool value)
        {
            _button.interactable = value;
        }

        public void SetSelected(bool value)
        {
            if (_outline != null)
                _outline.gameObject.SetActive(value);
        }

        private HandAuraGraphic CreateOutline()
        {
            var host = (RectTransform)transform;
            var go = new GameObject(OutlineName, typeof(RectTransform), typeof(CanvasRenderer), typeof(HandAuraGraphic));
            var rect = (RectTransform)go.transform;

            rect.SetParent(host, false);
            rect.anchorMin = host.pivot;
            rect.anchorMax = host.pivot;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.SetAsLastSibling();

            var aura = go.GetComponent<HandAuraGraphic>();

            aura.width = OutlineWidth;
            aura.cutout = 1f;
            aura.halftoneColor = OutlineHalftone;
            aura.inkColor = OutlineInk;
            aura.raycastTarget = false;
            aura.Bind(_outlineTargets);

            return aura;
        }

        public void SetIncomplete(string reason)
        {
            var hasReason = !string.IsNullOrEmpty(reason);

            if (incompleteText != null)
                incompleteText.text = reason;

            if (incompletePanel != null)
                incompletePanel.SetActive(hasReason);
        }
    }
}
