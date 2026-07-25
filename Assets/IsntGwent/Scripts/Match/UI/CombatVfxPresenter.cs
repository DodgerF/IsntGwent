using DG.Tweening;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using IsntGwent.Scripts.Messages;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class CombatVfxPresenter : MonoBehaviour
    {
        public GameObject projectilePrefab;

        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;

        private void Start()
        {
            _matchState.DamageDealt
                .Subscribe(Play)
                .AddTo(this);
        }

        private void Play(DamageInstance[] hits)
        {
            if (hits == null) return;

            foreach (var hit in hits)
                PlayHit(hit);
        }

        private void PlayHit(DamageInstance hit)
        {
            var target = _registry.Get(hit.TargetInstanceId);
            if (target == null) return;

            var source = _registry.Get(hit.SourceInstanceId);

            if (projectilePrefab == null || source == null)
            {
                target.HitReact();
                return;
            }

            var projectile = Instantiate(projectilePrefab, transform);
            projectile.transform.position = source.transform.position;

            projectile.transform
                .DOMove(target.transform.position, CardAnimConfig.ProjectileDuration)
                .SetEase(CardAnimConfig.ProjectileEase)
                .OnComplete(() =>
                {
                    if (target != null)
                        target.HitReact();

                    Destroy(projectile);
                });
        }
    }
}
