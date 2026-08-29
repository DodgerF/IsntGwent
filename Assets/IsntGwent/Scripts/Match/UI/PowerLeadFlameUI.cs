using IsntGwent.Scripts.Match.Client;
using IsntGwent.Scripts.Vfx;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class PowerLeadFlameUI : MonoBehaviour
    {
        public RectTransform ownTotalCircle;
        public RectTransform enemyTotalCircle;

        public string ownFlameId = "vfx_power_flame_own";
        public string enemyFlameId = "vfx_power_flame_enemy";
        public float flameTail = 0.9f;

        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly VfxService _vfx;

        private GameObject _flame;

        private void Start()
        {
            _matchState.OwnTotalPower
                .CombineLatest(
                    _matchState.EnemyTotalPower,
                    _matchState.IsGameEnded,
                    (own, enemy, ended) => ended ? 0 : Leader(own, enemy))
                .DistinctUntilChanged()
                .Subscribe(Apply)
                .AddTo(this);
        }

        private static int Leader(int own, int enemy)
        {
            if (own == enemy) return 0;
            if (own <= 0 && enemy <= 0) return 0;

            return own > enemy ? 1 : -1;
        }

        private void Apply(int side)
        {
            Extinguish(flameTail);

            if (side == 0) return;

            var target = side > 0 ? ownTotalCircle : enemyTotalCircle;
            var id = side > 0 ? ownFlameId : enemyFlameId;

            if (target == null || !_vfx.Has(id)) return;

            _flame = _vfx.SpawnFitted(id, target, target);

            if (_flame != null)
                _flame.transform.SetAsFirstSibling();
        }

        private void Extinguish(float tail)
        {
            if (_flame == null) return;

            _vfx.StopAndDespawn(_flame, tail);
            _flame = null;
        }

        private void OnDestroy() => Extinguish(0f);
    }
}
