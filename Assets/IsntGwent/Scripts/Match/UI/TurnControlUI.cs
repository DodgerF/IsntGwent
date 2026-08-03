using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class TurnControlUI : MonoBehaviour
    {
        public GameObject turnTracker;
        public Button passButton;

        [Inject] private readonly MatchState _matchState;

        private void Start()
        {
            _matchState.IsMyTurn
                .Subscribe(value => turnTracker.SetActive(value))
                .AddTo(this);

            _matchState.IsMyTurn
                .CombineLatest(
                    _matchState.IsActionPending,
                    _matchState.IsMatchPaused,
                    (isMyTurn, isPending, isPaused) => isMyTurn && !isPending && !isPaused)
                .Subscribe(value =>
                {
                    passButton.interactable = value;
                    passButton.gameObject.SetActive(value);
                })
                .AddTo(this);

            passButton.OnClickAsObservable()
                .Subscribe(_ => _matchState.PassRequested.OnNext(Unit.Default))
                .AddTo(this);
        }
    }
}
