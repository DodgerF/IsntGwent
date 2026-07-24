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
                .CombineLatest(_matchState.IsActionPending, (isMyTurn, isPending) => isMyTurn && !isPending)
                .Subscribe(value => passButton.interactable = value)
                .AddTo(this);

            passButton.OnClickAsObservable()
                .Subscribe(_ => _matchState.PassRequested.OnNext(Unit.Default))
                .AddTo(this);
        }
    }
}
