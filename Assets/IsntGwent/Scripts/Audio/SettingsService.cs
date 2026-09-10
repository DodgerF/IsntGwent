using UniRx;
using UnityEngine;
using UnityEngine.Audio;
using Zenject;

namespace IsntGwent.Scripts.Audio
{
    public class SettingsService : IInitializable
    {
        public readonly ReactiveProperty<float> Master = new(1f);
        public readonly ReactiveProperty<float> Music = new(1f);
        public readonly ReactiveProperty<float> Sfx = new(1f);
        public readonly ReactiveProperty<float> Ui = new(1f);

        private AudioMixer _mixer;

        public void Initialize()
        {
            _mixer = Resources.Load<AudioMixer>("Audio/GameMixer");

            Bind(Master, "vol.master", "MasterVol");
            Bind(Music, "vol.music", "MusicVol");
            Bind(Sfx, "vol.sfx", "SfxVol");
            Bind(Ui, "vol.ui", "UiVol");
        }

        private void Bind(ReactiveProperty<float> property, string prefKey, string exposed)
        {
            property.Value = Mathf.Clamp01(PlayerPrefs.GetFloat(prefKey, 1f));

            property.Subscribe(value =>
            {
                PlayerPrefs.SetFloat(prefKey, value);
                if (_mixer != null)
                    _mixer.SetFloat(exposed, LinearToDb(value));
            });
        }

        private static float LinearToDb(float value)
        {
            return value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        }
    }
}
