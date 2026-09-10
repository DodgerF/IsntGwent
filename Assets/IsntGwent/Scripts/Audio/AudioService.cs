using UnityEngine;
using UniRx;
using Random = UnityEngine.Random;

namespace IsntGwent.Scripts.Audio
{
    public class AudioService
    {
        private const int MaxStack = 3;
        private const float StackDetune = 0.04f;

        private readonly SoundDatabase _database;
        private readonly AudioPlayer _player;

        private string _pendingMusicId;

        public AudioService(SoundDatabase database, AudioPlayer player)
        {
            _database = database;
            _player = player;

            _database.OnLoaded
                .Where(loaded => loaded)
                .Subscribe(_ =>
                {
                    if (!string.IsNullOrEmpty(_pendingMusicId))
                        PlayMusic(_pendingMusicId);
                });
        }

        public void Play(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            var entry = _database.Get(id);
            if (entry == null) return;

            var clip = PickClip(entry);
            if (clip == null) return;

            if (entry.Category == SoundCategory.Music && entry.Loop)
            {
                _pendingMusicId = id;
                _player.PlayMusic(clip, entry.Volume, entry.Loop);
                return;
            }

            _player.PlayOneShot(clip, entry.Category, entry.Volume, PickPitch(entry));
        }

        public void PlayStack(string id, int count)
        {
            if (count <= 1)
            {
                Play(id);
                return;
            }

            if (string.IsNullOrEmpty(id)) return;

            var entry = _database.Get(id);
            if (entry == null) return;

            if (entry.Category == SoundCategory.Music && entry.Loop)
            {
                Play(id);
                return;
            }

            var layers = Mathf.Min(count, MaxStack);
            var volume = entry.Volume / Mathf.Sqrt(layers);
            var hasPitchRange = entry.PitchMax > entry.PitchMin;

            for (var i = 0; i < layers; i++)
            {
                var clip = PickClip(entry);
                if (clip == null) continue;

                var pitch = PickPitch(entry);
                if (!hasPitchRange)
                    pitch += (i - (layers - 1) * 0.5f) * StackDetune;

                _player.PlayOneShot(clip, entry.Category, volume, pitch);
            }
        }

        public void PlayMusic(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            if (!_database.OnLoaded.Value)
            {
                _pendingMusicId = id;
                return;
            }

            var entry = _database.Get(id);
            if (entry == null) return;

            var clip = PickClip(entry);
            if (clip == null) return;

            _pendingMusicId = id;
            _player.PlayMusic(clip, entry.Volume, entry.Loop);
        }

        public void StopMusic()
        {
            _pendingMusicId = null;
            _player.StopMusic();
        }

        private static float PickPitch(SoundEntryDefinition entry)
        {
            return entry.PitchMax > entry.PitchMin
                ? Random.Range(entry.PitchMin, entry.PitchMax)
                : entry.PitchMin;
        }

        private static UnityEngine.AudioClip PickClip(SoundEntryDefinition entry)
        {
            var clips = entry.LoadedClips;
            if (clips == null || clips.Length == 0) return null;
            return clips.Length == 1 ? clips[0] : clips[Random.Range(0, clips.Length)];
        }
    }
}
