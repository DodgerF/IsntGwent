using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class MatchUI : MonoBehaviour
    {
        public GameObject waitingImage;
        public GameObject ui;

        [Inject] private readonly MatchClientHandler _handler;
        [Inject] private readonly MatchState _matchState;

        private void Start()
        {
            _matchState.IsWaitingImageActive
                .Subscribe(value =>
                {
                    waitingImage.SetActive(value);
                    ui.SetActive(!value);
                })
                .AddTo(this);

            _handler.SendReadyMessage();
        }
    }
}
