using UnityEngine;
using UnityEngine.Audio;

namespace IsntGwent.Scripts.Audio
{
    public class AudioPlayer : MonoBehaviour
    {
        private const int PoolSize = 8;

        private AudioMixerGroup _musicGroup;
        private AudioMixerGroup _sfxGroup;
        private AudioMixerGroup _uiGroup;

        private AudioSource _musicSource;
        private AudioSource[] _oneShots;
        private int _next;

        private void Awake()
        {
            var mixer = Resources.Load<AudioMixer>("Audio/GameMixer");
            if (mixer != null)
            {
                _musicGroup = FindGroup(mixer, "Master/Music");
                _sfxGroup = FindGroup(mixer, "Master/Sfx");
                _uiGroup = FindGroup(mixer, "Master/Ui");
            }

            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.outputAudioMixerGroup = _musicGroup;

            _oneShots = new AudioSource[PoolSize];
            for (var i = 0; i < PoolSize; i++)
            {
                var source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                _oneShots[i] = source;
            }
        }

        public void PlayOneShot(AudioClip clip, SoundCategory category, float volume, float pitch)
        {
            if (clip == null) return;

            var source = _oneShots[_next];
            _next = (_next + 1) % _oneShots.Length;

            source.outputAudioMixerGroup = category switch
            {
                SoundCategory.Music => _musicGroup,
                SoundCategory.Ui => _uiGroup,
                _ => _sfxGroup
            };
            source.pitch = pitch;
            source.PlayOneShot(clip, volume);
        }

        public void PlayMusic(AudioClip clip, float volume, bool loop)
        {
            if (clip == null) return;
            if (_musicSource.clip == clip && _musicSource.isPlaying) return;

            _musicSource.clip = clip;
            _musicSource.volume = volume;
            _musicSource.loop = loop;
            _musicSource.Play();
        }

        public void StopMusic()
        {
            _musicSource.Stop();
        }

        private static AudioMixerGroup FindGroup(AudioMixer mixer, string path)
        {
            var groups = mixer.FindMatchingGroups(path);
            return groups.Length > 0 ? groups[0] : null;
        }
    }
}
