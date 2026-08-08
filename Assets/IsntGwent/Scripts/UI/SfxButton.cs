using IsntGwent.Scripts.Audio;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.UI
{
    [AddComponentMenu("IsntGwent/Sfx Button")]
    public class SfxButton : Button
    {
        public string clickSoundId = "ui_click";
        public string hoverSoundId = "ui_button_hover";
        public string hoverOutSoundId = "";

        [Inject] private readonly AudioService _audio;

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                PlayClick();

            base.OnPointerClick(eventData);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            PlayClick();

            base.OnSubmit(eventData);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            base.OnPointerEnter(eventData);

            if (!IsActive() || !IsInteractable()) return;
            if (eventData is ExtendedPointerEventData ext && ext.pointerType != UIPointerType.MouseOrPen) return;

            _audio?.Play(hoverSoundId);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);

            if (string.IsNullOrEmpty(hoverOutSoundId)) return;
            if (!IsActive() || !IsInteractable()) return;
            if (eventData is ExtendedPointerEventData ext && ext.pointerType != UIPointerType.MouseOrPen) return;

            _audio?.Play(hoverOutSoundId);
        }

        private void PlayClick()
        {
            if (!IsActive() || !IsInteractable()) return;

            _audio?.Play(clickSoundId);
        }
    }
}
