using IsntGwent.Scripts.Match.Client;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class HpPanelUI : MonoBehaviour
    {
        public PlayerHpUI ownHp;
        public PlayerHpUI enemyHp;

        [Inject] private readonly MatchState _matchState;

        private void Start()
        {
            _matchState.MyHp
                .Subscribe(value => ownHp.SetHp(value))
                .AddTo(this);

            _matchState.EnemyHp
                .Subscribe(value => enemyHp.SetHp(value))
                .AddTo(this);
        }
    }
}
