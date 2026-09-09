using IsntGwent.Scripts.Localization;
using IsntGwent.Scripts.Match.Client;
using IsntGwent.Scripts.Tutorial.Client;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class TurnControlUI : MonoBehaviour
    {
        public Button passButton;
        public TextMeshProUGUI passButtonLabel;
        public string passText = "Pass";
        public string enemyTurnText = "Enemy Turn";

        [Inject] private readonly MatchState _matchState;
        [InjectOptional] private readonly TutorialService _tutorial;

        private void Start()
        {
            var mustPlay = _matchState.PendingPlays
                .ObserveCountChanged(true)
                .CombineLatest(_matchState.IsPendingMine, (count, isMine) => isMine && count > 0);

            _matchState.IsMyTurn
                .CombineLatest(
                    _matchState.IsActionPending,
                    _matchState.IsMatchPaused,
                    mustPlay,
                    (isMyTurn, isPending, isPaused, hasPending) =>
                        isMyTurn && !isPending && !isPaused && !hasPending)
                .Subscribe(canPass => passButton.interactable = canPass)
                .AddTo(this);

            _matchState.IsMyTurn
                .Subscribe(isMyTurn => passButtonLabel.text = Loc.T(isMyTurn ? passText : enemyTurnText))
                .AddTo(this);

            passButton.OnClickAsObservable()
                .Subscribe(_ => OnPassClicked())
                .AddTo(this);
        }

        private void OnPassClicked()
        {
            if (_tutorial != null && !_tutorial.EvaluatePass(
                    _matchState.Hand.Count == 0,
                    _matchState.IsEnemyPassed.Value,
                    _matchState.OwnTotalPower.Value > _matchState.EnemyTotalPower.Value))
            {
                _tutorial.RejectPass();
                return;
            }

            _matchState.PassRequested.OnNext(Unit.Default);
        }
    }
}
