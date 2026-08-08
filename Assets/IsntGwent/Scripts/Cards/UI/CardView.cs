using DG.Tweening;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    public enum CardMode { InHand, OnBoard }

    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverSoundInterval = 0.08f;

        private static float _lastHoverSoundTime;

        public Image cardImage;
        public TextMeshProUGUI powerText;
        public GameObject meleeIcon;
        public GameObject rangedIcon;
        public Image targetHighlight;

        public bool hoverSfx = true;
        public string hoverSoundId = "ui_hover";
        public string hoverOutSoundId = "ui_hover_out";

        [Inject] private readonly AudioService _audio;

        private bool _hovered;

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
                if (instance is UnitInstance unitInstance)
                {
                    unitInstance.CurrentPower
                        .Subscribe(power => powerText.text = power.ToString())
                        .AddTo(this);

                    unitInstance.CurrentPower
                        .Skip(1)
                        .Subscribe(_ => PowerPop())
                        .AddTo(this);
                }

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
        public enum TargetHighlightState { None, Available, Selected }
        public void SetTargetHighlight(TargetHighlightState state)
        {
            switch (state)
            {
                case TargetHighlightState.None:
                    targetHighlight.color = new Color(0f, 0f, 0f, 0.0f);
                    break;
                case TargetHighlightState.Available:
                    targetHighlight.color = new Color(0.57f, 0.635f, 1f, 0.5f);
                    break;
                case TargetHighlightState.Selected:
                    targetHighlight.color = new Color(0.6f, 1f, 0.4f, 0.5f);
                    break;
            }
        }
        public void SetSelected(bool selected)
        {
            gameObject.SetActive(!selected);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanPlayHover(eventData)) return;

            _hovered = true;
            PlayHover(hoverSoundId);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_hovered) return;

            _hovered = false;
            PlayHover(hoverOutSoundId);
        }

        private bool CanPlayHover(PointerEventData eventData)
        {
            if (!hoverSfx) return false;

            return eventData is not ExtendedPointerEventData ext || ext.pointerType == UIPointerType.MouseOrPen;
        }

        private void PlayHover(string soundId)
        {
            if (!hoverSfx) return;
            if (Time.unscaledTime - _lastHoverSoundTime < HoverSoundInterval) return;

            _lastHoverSoundTime = Time.unscaledTime;
            _audio?.Play(soundId);
        }

        private void OnDisable()
        {
            _hovered = false;
        }

        public void HitReact()
        {
            if (!Application.isPlaying || cardImage == null) return;

            var shaken = cardImage.transform;
            shaken.DOKill(true);
            shaken.DOShakePosition(
                CardAnimConfig.HitReactDuration,
                CardAnimConfig.HitShakeStrength);
        }

        private void PowerPop()
        {
            if (!Application.isPlaying || powerText == null) return;

            var popped = powerText.transform;
            popped.DOKill(true);
            popped.localScale = Vector3.one;
            popped.DOPunchScale(
                Vector3.one * (CardAnimConfig.PowerPopScale - 1f),
                CardAnimConfig.PowerPopDuration,
                1,
                0.5f);
        }
    }
}