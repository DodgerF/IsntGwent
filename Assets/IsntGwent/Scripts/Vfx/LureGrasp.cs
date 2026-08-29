using System;
using Coffee.UIExtensions;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Vfx
{
    public class LureGrasp : MonoBehaviour
    {
        public RectTransform hand;
        public Image handImage;
        public Sprite openSprite;
        public Sprite gripSprite;
        public VfxParticleRope sleeve;

        public float startScale = 0.55f;
        public float gripScale = 1.14f;
        public float holdScale = 0.98f;
        public float grabInset = 0.34f;
        public float wristOffset = 42f;
        public float sleeveSag = 26f;
        public float retract = 0.35f;

        private UIParticle _particle;
        private Sequence _sequence;
        private Transform _source;
        private RectTransform _card;
        private Vector3 _offset;
        private Color _handColor;
        private float _release;
        private bool _carrying;

        private void Awake()
        {
            _particle = GetComponent<UIParticle>();
            if (handImage != null) _handColor = handImage.color;
        }

        public void Play(Transform source, RectTransform card, Action onGrip,
            float reach, float grip, float drag, float release)
        {
            if (source == null || card == null || hand == null) return;

            _sequence?.Kill();
            _source = source;
            _card = card;
            _release = release;
            _carrying = false;

            var from = source.position;
            var to = GrabPoint(card, from);
            var toward = to - from;

            hand.position = from;
            hand.localRotation = Quaternion.Euler(0f, 0f,
                Mathf.Atan2(toward.y, toward.x) * Mathf.Rad2Deg);
            hand.localScale = Vector3.one * startScale;

            if (handImage != null) handImage.sprite = openSprite;
            SetAlpha(0f);

            if (sleeve != null)
                sleeve.Bind(source, Vector3.zero, hand, new Vector3(-wristOffset, 0f, 0f),
                    0f, sleeveSag, _particle != null ? _particle.scale : 1f);

            _sequence = DOTween.Sequence();
            _sequence.Append(hand.DOMove(to, reach).SetEase(Ease.InQuad));
            _sequence.Join(hand.DOScale(Vector3.one, reach).SetEase(Ease.OutQuad));
            _sequence.Join(DOVirtual.Float(0f, 1f, reach * 0.45f, SetAlpha));
            _sequence.AppendCallback(() => Grip(onGrip));
            _sequence.Append(hand.DOScale(Vector3.one * gripScale, grip * 0.4f).SetEase(Ease.OutQuad));
            _sequence.Append(hand.DOScale(Vector3.one * holdScale, grip * 0.6f).SetEase(Ease.InQuad));
            _sequence.AppendInterval(drag);
            _sequence.AppendCallback(Retract);
            _sequence.Append(DOVirtual.Float(1f, 0f, release, SetAlpha));
        }

        private Vector3 GrabPoint(RectTransform card, Vector3 from)
        {
            var center = card.position;
            var toward = from - center;
            if (toward.sqrMagnitude < 0.0001f) return center;

            var size = Vector2.Scale(card.rect.size, card.lossyScale);
            var dir = ((Vector2)toward).normalized;
            var reach = Mathf.Min(
                Mathf.Abs(dir.x) > 0.0001f ? Mathf.Abs(size.x * 0.5f / dir.x) : float.MaxValue,
                Mathf.Abs(dir.y) > 0.0001f ? Mathf.Abs(size.y * 0.5f / dir.y) : float.MaxValue);

            return center + (Vector3)(dir * (reach * (1f - grabInset)));
        }

        private void Grip(Action onGrip)
        {
            if (handImage != null) handImage.sprite = gripSprite;

            if (_card != null)
            {
                _offset = hand.position - _card.position;
                _carrying = true;
            }

            onGrip?.Invoke();
        }

        private void Retract()
        {
            _carrying = false;
            if (_source == null || hand == null) return;

            hand.DOMove(Vector3.Lerp(hand.position, _source.position, retract), _release)
                .SetEase(Ease.InQuad);
        }

        private void LateUpdate()
        {
            if (!_carrying) return;

            if (_card == null)
            {
                _carrying = false;
                return;
            }

            hand.position = _card.position + _offset;
            if (sleeve != null) sleeve.Rebuild();
        }

        private void SetAlpha(float value)
        {
            if (handImage != null)
            {
                var color = _handColor;
                color.a *= value;
                handImage.color = color;
            }

            if (sleeve != null) sleeve.Alpha = value;
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
            _carrying = false;
            _card = null;
            _source = null;

            if (hand != null) hand.DOKill();
            if (sleeve != null) sleeve.Release();
        }
    }
}
