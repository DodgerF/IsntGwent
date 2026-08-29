using System.Collections.Generic;
using DG.Tweening;
using IsntGwent.Scripts.Vfx;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Match.UI
{
    public class PlayerHpUI : MonoBehaviour
    {
        public GameObject heartPrefab;
        public float heartSpacing = 74f;
        public int maxHp = 2;

        public string breakVfxId = "vfx_heart_break";
        public float crackDuration = 0.16f;
        public float crackScale = 1.28f;
        public float shatterDuration = 0.55f;
        public float shardSpread = 30f;
        public float shardLift = 14f;
        public float shardDrop = 52f;
        public float shardSpin = 38f;
        public Color crackColor = new(1f, 0.94f, 0.9f, 1f);

        [Inject(Optional = true)] private readonly VfxService _vfx;

        private readonly List<Image> _hearts = new();
        private int _hp = -1;

        public void SetHp(int hp)
        {
            Build(Mathf.Max(hp, maxHp));

            var count = _hearts.Count;

            if (_hp < 0)
            {
                for (var i = 0; i < count; i++)
                    Restore(i, i >= count - hp);

                _hp = hp;
                return;
            }

            for (var i = count - _hp; i < count - hp; i++)
                Break(i);

            for (var i = count - hp; i < count - _hp; i++)
                Restore(i, true);

            _hp = hp;
        }

        private void Build(int count)
        {
            if (_hearts.Count == count) return;

            while (_hearts.Count > count)
            {
                var last = _hearts[^1];
                if (last != null) Destroy(last.gameObject);
                _hearts.RemoveAt(_hearts.Count - 1);
            }

            while (_hearts.Count < count)
            {
                var heart = Instantiate(heartPrefab, transform);
                _hearts.Add(heart.GetComponent<Image>());
            }

            var totalWidth = (count - 1) * heartSpacing;
            var startX = -totalWidth / 2f;

            for (var i = 0; i < count; i++)
                _hearts[i].transform.localPosition = new Vector3(startX + i * heartSpacing, 0f, 0f);
        }

        private void Restore(int index, bool alive)
        {
            var heart = _hearts[index];
            if (heart == null) return;

            heart.transform.DOKill();
            heart.DOKill();

            heart.transform.localScale = Vector3.one;
            heart.transform.localRotation = Quaternion.identity;
            heart.enabled = alive;
        }

        private void Break(int index)
        {
            if (index < 0 || index >= _hearts.Count) return;

            var heart = _hearts[index];
            if (heart == null || !heart.enabled) return;

            var rect = (RectTransform)heart.transform;
            var baseColor = heart.color;

            rect.DOKill();
            heart.DOKill();

            DOTween.Sequence()
                .SetTarget(rect)
                .Append(rect.DOScale(Vector3.one * crackScale, crackDuration).SetEase(Ease.OutBack))
                .Join(heart.DOColor(crackColor, crackDuration))
                .Append(rect.DOShakeRotation(crackDuration, new Vector3(0f, 0f, 16f), 14, 90f))
                .AppendCallback(() => Shatter(rect, heart, baseColor))
                .SetLink(gameObject);
        }

        private void Shatter(RectTransform source, Image heart, Color baseColor)
        {
            if (_vfx != null && _vfx.Has(breakVfxId))
                _vfx.PlayFitted(breakVfxId, source);

            Fling(MakeShard(source, heart, 0), -1f);
            Fling(MakeShard(source, heart, 1), 1f);

            heart.enabled = false;
            heart.color = baseColor;
            source.localScale = Vector3.one;
            source.localRotation = Quaternion.identity;
        }

        private Image MakeShard(RectTransform source, Image heart, int fillOrigin)
        {
            var go = new GameObject("HeartShard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;

            rect.SetParent(source.parent, false);
            rect.SetSiblingIndex(source.GetSiblingIndex());
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;
            rect.localPosition = source.localPosition;
            rect.localScale = source.localScale;
            rect.localRotation = source.localRotation;

            var shard = go.GetComponent<Image>();
            shard.sprite = heart.sprite;
            shard.material = heart.material;
            shard.color = heart.color;
            shard.preserveAspect = heart.preserveAspect;
            shard.raycastTarget = false;
            shard.type = Image.Type.Filled;
            shard.fillMethod = Image.FillMethod.Horizontal;
            shard.fillOrigin = fillOrigin;
            shard.fillAmount = 0.5f;

            return shard;
        }

        private void Fling(Image shard, float direction)
        {
            var rect = (RectTransform)shard.transform;
            var origin = rect.localPosition;
            var rise = shatterDuration * 0.3f;
            var fall = shatterDuration - rise;

            DOTween.Sequence()
                .SetTarget(rect)
                .Append(rect.DOLocalMoveY(origin.y + shardLift, rise).SetEase(Ease.OutQuad))
                .Append(rect.DOLocalMoveY(origin.y - shardDrop, fall).SetEase(Ease.InQuad))
                .Insert(0f, rect.DOLocalMoveX(origin.x + direction * shardSpread, shatterDuration).SetEase(Ease.OutQuad))
                .Insert(0f, rect.DOLocalRotate(new Vector3(0f, 0f, -direction * shardSpin), shatterDuration))
                .Insert(0f, rect.DOScale(rect.localScale * 0.82f, shatterDuration))
                .Insert(shatterDuration * 0.35f, shard.DOFade(0f, shatterDuration * 0.65f).SetEase(Ease.InQuad))
                .OnComplete(() => Destroy(shard.gameObject))
                .SetLink(gameObject);
        }
    }
}
