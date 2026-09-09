using IsntGwent.Scripts.Localization;
using IsntGwent.Scripts.Lobby.Client;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class WaitingStatusView : MonoBehaviour
    {
        [Inject] private PlaySession _play;

        [SerializeField] private TextMeshProUGUI statusText;

        private void Start()
        {
            _play.Phase
                .Where(phase => phase != PlayPhase.None)
                .Subscribe(phase => statusText.text = Loc.T(Describe(phase)))
                .AddTo(this);
        }

        private static string Describe(PlayPhase phase)
        {
            return phase switch
            {
                PlayPhase.Searching => "Looking for an opponent...",
                PlayPhase.WaitingForFriend => "Waiting for a friend...",
                _ => "Loading Match..."
            };
        }
    }
}
