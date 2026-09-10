using DG.Tweening;
using IsntGwent.Scripts.Cards.Client;
using IsntGwent.Scripts.Match;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.UI;
using IsntGwent.Scripts.Match.Client;
using IsntGwent.Scripts.Messages;
using IsntGwent.Scripts.Vfx;
using UniRx;
using UnityEngine;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class CombatVfxPresenter : MonoBehaviour
    {
        private const string BoltVfxId = "vfx_damage_bolt";
        private const string ImpactVfxId = "vfx_damage_impact";
        private const string DevourVfxId = "vfx_devour";
        private const string LureGraspVfxId = "vfx_lure_grasp";

        [Inject] private readonly MatchState _matchState;
        [Inject] private readonly CardViewRegistry _registry;
        [Inject] private readonly VfxService _vfx;

        private BoardRowView[] _rows;

        private void Start()
        {
            _rows = FindObjectsByType<BoardRowView>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            _matchState.DamageDealt
                .Subscribe(PlayHits)
                .AddTo(this);

            _matchState.UnitsLinked
                .Subscribe(PlayLinks)
                .AddTo(this);
        }

        private void PlayHits(DamageInstance[] hits)
        {
            if (hits == null) return;

            foreach (var hit in hits)
                PlayHit(hit);
        }

        private void PlayHit(DamageInstance hit)
        {
            var target = _registry.Get(hit.TargetInstanceId);
            if (target == null) return;

            if (hit.Kind == DamageKind.Weather)
            {
                WeatherImpact(target, hit.SourceCardId);
                return;
            }

            var source = _registry.Get(hit.SourceInstanceId);
            if (source == null)
            {
                Impact(target);
                return;
            }

            var bolt = _vfx.Spawn(BoltVfxId);
            if (bolt == null)
            {
                Impact(target);
                return;
            }

            bolt.transform.position = source.transform.position;
            bolt.transform
                .DOMove(target.transform.position, CardAnimConfig.ProjectileDuration)
                .SetEase(CardAnimConfig.ProjectileEase)
                .OnComplete(() =>
                {
                    Impact(target);
                    _vfx.StopAndDespawn(bolt, CardAnimConfig.BoltTailDuration);
                });
        }

        private void Impact(CardView target)
        {
            if (target == null) return;

            target.HitReact();
            _vfx.Play(ImpactVfxId, target.transform.position);
        }

        private void WeatherImpact(CardView target, string cardId)
        {
            target.HitReact();
            _vfx.PlayFitted(WeatherVfx.Hit(_vfx, cardId), (RectTransform)target.transform);
        }

        private void PlayLinks(UnitLinkData[] links)
        {
            if (links == null) return;

            foreach (var link in links)
                PlayLink(link);
        }

        private void PlayLink(UnitLinkData link)
        {
            var target = _registry.Get(link.TargetInstanceId);
            if (target == null) return;

            if (link.Kind == UnitLinkKind.Devour)
            {
                target.HitReact();
                _vfx.PlayFitted(DevourVfxId, (RectTransform)target.transform);
                return;
            }

            var source = _registry.Get(link.SourceInstanceId);
            if (source == null) return;

            PlayLure(source, target, link);
        }

        private void PlayLure(CardView source, CardView target, UnitLinkData link)
        {
            var drag = DragDuration(target, link);

            var instance = _vfx.Spawn(LureGraspVfxId);
            var grasp = instance != null ? instance.GetComponent<LureGrasp>() : null;
            if (grasp == null)
            {
                Tug(source, target, link);
                return;
            }

            instance.transform.position = target.transform.position;

            grasp.Play(
                source.transform,
                (RectTransform)target.transform,
                () => Tug(source, target, link),
                CardAnimConfig.LureReachDuration,
                CardAnimConfig.LureGripDuration,
                drag,
                CardAnimConfig.LureReleaseDuration);

            _vfx.Despawn(instance,
                CardAnimConfig.LureReachDuration + CardAnimConfig.LureGripDuration
                + drag + CardAnimConfig.LureReleaseDuration);
        }

        private float DragDuration(CardView target, UnitLinkData link)
        {
            var current = target.GetComponentInParent<BoardRowView>();
            var sameRow = current != null && current.BoardRow == link.TargetRow;

            return sameRow ? CardAnimConfig.RowLayoutDuration : CardAnimConfig.PlayFlightDuration;
        }

        private void Tug(CardView source, CardView target, UnitLinkData link)
        {
            if (source == null || target == null) return;

            target.TugReact(source.transform.position - target.transform.position);

            PullToSlot(target, link);
        }

        private void PullToSlot(CardView target, UnitLinkData link)
        {
            if (link.TargetRow == RowType.None || !BoardConfig.IsValidSlot(link.TargetSlot)) return;
            if (_rows == null) return;

            var current = target.GetComponentInParent<BoardRowView>();
            if (current == null) return;

            foreach (var row in _rows)
            {
                if (row == null || !row.HasSlots) continue;
                if (row.OwnSide != current.OwnSide || row.BoardRow != link.TargetRow) continue;

                row.PlaceCard(target.gameObject, link.TargetSlot);
                return;
            }
        }
    }
}
