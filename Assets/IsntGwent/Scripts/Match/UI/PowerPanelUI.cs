using IsntGwent.Scripts.Match.Client;
using TMPro;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class PowerPanelUI : MonoBehaviour
    {
        public TextMeshProUGUI ownMeleePowerText;
        public TextMeshProUGUI ownRangedPowerText;
        public TextMeshProUGUI ownTotalPowerText;

        public TextMeshProUGUI enemyMeleePowerText;
        public TextMeshProUGUI enemyRangedPowerText;
        public TextMeshProUGUI enemyTotalPowerText;

        public TextMeshProUGUI ownCardCounter;
        public TextMeshProUGUI enemyCardCounter;

        [Inject] private readonly MatchState _matchState;

        private void Start()
        {
            Bind(_matchState.OwnMeleePower, ownMeleePowerText);
            Bind(_matchState.OwnRangedPower, ownRangedPowerText);
            Bind(_matchState.OwnTotalPower, ownTotalPowerText);

            Bind(_matchState.EnemyMeleePower, enemyMeleePowerText);
            Bind(_matchState.EnemyRangedPower, enemyRangedPowerText);
            Bind(_matchState.EnemyTotalPower, enemyTotalPowerText);

            if (ownCardCounter != null)
                _matchState.Hand
                    .ObserveCountChanged()
                    .Subscribe(count => ownCardCounter.text = count.ToString())
                    .AddTo(this);

            Bind(_matchState.EnemyCardAmount, enemyCardCounter);
        }

        private void Bind(IReadOnlyReactiveProperty<int> value, TextMeshProUGUI text)
        {
            if (text == null) return;

            value
                .Subscribe(v => text.text = v.ToString())
                .AddTo(this);
        }
    }
}
