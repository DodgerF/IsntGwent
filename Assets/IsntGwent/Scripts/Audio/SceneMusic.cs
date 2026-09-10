using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Audio
{
    public class SceneMusic : MonoBehaviour
    {
        public string trackId = "music_menu";

        [Inject] private readonly AudioService _audio;

        private void Start()
        {
            if (!string.IsNullOrEmpty(trackId))
                _audio.PlayMusic(trackId);
        }
    }
}
