using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Audio
{
    [RequireComponent(typeof(Button))]
    public class ButtonClickSfx : MonoBehaviour, IPointerEnterHandler
    {
        public string soundId = "ui_click";
        public string hoverSoundId = "ui_button_hover";

        [Inject] private readonly AudioService _audio;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void Start()
        {
            if (_button == null) return;

            _button.OnClickAsObservable()
                .Subscribe(_ => _audio.Play(soundId))
                .AddTo(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_audio == null || _button == null || !_button.interactable) return;
            if (eventData is ExtendedPointerEventData ext && ext.pointerType != UIPointerType.MouseOrPen) return;

            _audio.Play(hoverSoundId);
        }
    }
}
