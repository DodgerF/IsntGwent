using System.Collections.Generic;
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
        public bool useSharedColors = true;

        [Inject] private readonly AudioService _audio;

        private Graphic[] _content;
        private float[] _contentAlpha;

        protected override void Awake()
        {
            base.Awake();

            if (useSharedColors)
            {
                transition = Transition.ColorTint;
                colors = ButtonPalette.Default;
            }

            CacheContent();
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            base.DoStateTransition(state, instant);

            FadeContent(IsInteractable() ? 1f : ButtonPalette.DisabledContentAlpha);
        }

        private void CacheContent()
        {
            var graphics = GetComponentsInChildren<Graphic>(true);
            var content = new List<Graphic>(graphics.Length);

            foreach (var graphic in graphics)
            {
                if (graphic == targetGraphic || graphic == image) continue;

                content.Add(graphic);
            }

            _content = content.ToArray();
            _contentAlpha = new float[_content.Length];

            for (var i = 0; i < _content.Length; i++)
                _contentAlpha[i] = _content[i].color.a;
        }

        private void FadeContent(float factor)
        {
            if (_content == null) return;

            for (var i = 0; i < _content.Length; i++)
            {
                var graphic = _content[i];
                if (graphic == null) continue;

                var color = graphic.color;
                graphic.color = new Color(color.r, color.g, color.b, _contentAlpha[i] * factor);
            }
        }

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
