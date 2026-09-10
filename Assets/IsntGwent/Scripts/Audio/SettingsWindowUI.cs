using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Audio
{
    public class SettingsWindowUI : MonoBehaviour
    {
        public Slider masterSlider;
        public Slider musicSlider;
        public Slider sfxSlider;
        public Slider uiSlider;
        public Button closeButton;

        [Inject] private readonly SettingsService _settings;

        private void Start()
        {
            Bind(masterSlider, _settings.Master);
            Bind(musicSlider, _settings.Music);
            Bind(sfxSlider, _settings.Sfx);
            Bind(uiSlider, _settings.Ui);

            if (closeButton != null)
                closeButton.OnClickAsObservable()
                    .Subscribe(_ => gameObject.SetActive(false))
                    .AddTo(this);
        }

        private void Bind(Slider slider, ReactiveProperty<float> property)
        {
            if (slider == null) return;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = property.Value;

            slider.OnValueChangedAsObservable()
                .Subscribe(value => property.Value = value)
                .AddTo(this);

            property
                .Subscribe(value =>
                {
                    if (!Mathf.Approximately(slider.value, value))
                        slider.value = value;
                })
                .AddTo(this);
        }

        public void Show()
        {
            gameObject.SetActive(true);
        }
    }
}
