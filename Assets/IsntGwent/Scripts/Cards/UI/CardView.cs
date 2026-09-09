using DG.Tweening;
using IsntGwent.Scripts.Audio;
using IsntGwent.Scripts.Cards.Definitions;
using IsntGwent.Scripts.Cards.Runtime;
using IsntGwent.Scripts.Vfx;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Zenject;

namespace IsntGwent.Scripts.Cards.UI
{
    public enum CardMode { InHand, OnBoard, InGraveyard }

    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private const float HoverSoundInterval = 0.08f;
        private const float ScaleEpsilon = 0.0001f;
        private const string BuffVfxId = "vfx_buff";

        public static readonly Vector2 NativeSize = new(100f, 150f);

        private static float _lastHoverSoundTime;

        public Image cardImage;
        public GameObject powerBack;
        public TextMeshProUGUI powerText;
        public GameObject armorRoot;
        public TextMeshProUGUI armorText;
        public Image targetHighlight;
        public RectTransform visual;

        public bool hoverSfx = true;
        public bool hoverScale;
        public bool trayHover;
        public string hoverSoundId = "ui_hover";
        public string hoverOutSoundId = "ui_hover_out";

        [Inject] private readonly AudioService _audio;
        [InjectOptional] private readonly VfxService _vfx;

        private readonly CompositeDisposable _cardBindings = new();
        private readonly object _scaleKey = new();
        private readonly object _dimKey = new();
        private readonly object _raiseKey = new();

        private bool _hovered;
        private bool _pointerInside;
        private bool _picked;
        private bool _dimmed;
        private bool _raised;
        private Vector3 _baseScale = Vector3.one;
        private CanvasGroup _group;
        private Image _glow;
        private Image _frame;
        private float _raiseT;
        private float _raiseLift;
        private float _raiseScale;
        private bool _visualApplied;

        public CardInstance Instance { get; private set; }

        public CardMode mode = CardMode.InHand;

        public void Setup(CardDefinition definition) => Setup(CardFactory.Create(definition));

        public void Setup(CardInstance instance)
        {
            _cardBindings.Clear();

            Instance = instance;
            var cardDefinition = instance.Definition;

            var spritePath = "Sprites/Cards/" + cardDefinition.ImageName;
            cardImage.sprite = Resources.Load<Sprite>(spritePath);

            if (instance is UnitInstance unitInstance)
            {
                powerText.gameObject.SetActive(true);
                if (powerBack != null) powerBack.SetActive(true);

                unitInstance.CurrentPower
                    .CombineLatest(unitInstance.BasePower, (power, basePower) => (power, basePower))
                    .Subscribe(e =>
                    {
                        powerText.text = e.power.ToString();
                        powerText.color = PowerColor(e.power, e.basePower);
                    })
                    .AddTo(_cardBindings);

                unitInstance.CurrentPower
                    .Skip(1)
                    .Subscribe(_ => PowerPop())
                    .AddTo(_cardBindings);

                unitInstance.CurrentPower
                    .Pairwise()
                    .Where(pair => pair.Current > pair.Previous)
                    .Subscribe(_ => PlayBuff())
                    .AddTo(_cardBindings);

                unitInstance.Armor
                    .Subscribe(ShowArmor)
                    .AddTo(_cardBindings);

                unitInstance.Armor
                    .Pairwise()
                    .Where(pair => pair.Current > pair.Previous)
                    .Subscribe(_ => _audio?.Play("unit_armor"))
                    .AddTo(_cardBindings);
            }
            else
            {
                powerText.gameObject.SetActive(false);
                if (powerBack != null) powerBack.SetActive(false);
                ShowArmor(0);
            }
        }

        private void ShowArmor(int armor)
        {
            armorRoot.SetActive(armor > 0);
            armorText.text = armor.ToString();
        }
        public static Color PowerColor(int power, int basePower)
        {
            if (power < basePower) return CardAnimConfig.PowerLoweredColor;
            if (power > basePower) return CardAnimConfig.PowerRaisedColor;

            return CardAnimConfig.PowerBaseColor;
        }

        public enum TargetHighlightState
        {
            None,
            Available,
            Selected,
            PredictedHostile,
            PredictedFriendly
        }

        public void SetTargetHighlight(TargetHighlightState state)
        {
            if (targetHighlight != null)
                targetHighlight.color = HighlightPalette.Hidden;

            if (state == TargetHighlightState.None && _glow == null) return;

            EnsureGlow();

            var target = state switch
            {
                TargetHighlightState.Available => HighlightPalette.Choosable,
                TargetHighlightState.Selected => HighlightPalette.Selected,
                TargetHighlightState.PredictedHostile => HighlightPalette.Damage,
                TargetHighlightState.PredictedFriendly => HighlightPalette.Support,
                _ => HighlightPalette.Hidden,
            };

            _glow.DOKill();

            if (!Application.isPlaying)
            {
                _glow.color = target;
                return;
            }

            _glow.DOColor(target, CardAnimConfig.SlotHighlightDuration);
        }

        private void EnsureGlow()
        {
            if (_glow != null) return;

            _glow = GlowSprite.Create(VisualRect, "CardGlow");
            GlowSprite.SetWidth(_glow, HighlightPalette.CardGlowWidth);

            var rt = (RectTransform)_glow.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-HighlightPalette.CardGlowWidth, -HighlightPalette.CardGlowWidth);
            rt.offsetMax = new Vector2(HighlightPalette.CardGlowWidth, HighlightPalette.CardGlowWidth);
            rt.SetAsFirstSibling();
        }
        public void SetSelected(bool selected)
        {
            _picked = selected;
            ApplyRaise();
        }

        public void SetHovered(bool hovered)
        {
            if (_pointerInside == hovered) return;

            _pointerInside = hovered;
            ApplyRaise();
        }

        public void SetDimmed(bool dimmed)
        {
            if (_dimmed == dimmed) return;

            _dimmed = dimmed;

            var group = Group();
            var target = dimmed ? CardAnimConfig.DimAlpha : 1f;

            DOTween.Kill(_dimKey);

            if (!Application.isPlaying)
            {
                group.alpha = target;
                return;
            }

            DOTween.To(() => group.alpha, value => group.alpha = value, target, CardAnimConfig.DimDuration)
                .SetTarget(_dimKey);
        }

        public CanvasGroup Group()
        {
            if (_group == null && !TryGetComponent(out _group))
                _group = gameObject.AddComponent<CanvasGroup>();

            return _group;
        }

        public float BaseScale => _baseScale.x;

        public void SetBaseScale(float scale) => SetBaseScale(scale, 0f, CardAnimConfig.RowLayoutEase);

        public void SetBaseScale(float scale, float duration, Ease ease)
        {
            var target = Vector3.one * scale;

            if ((_baseScale - target).sqrMagnitude <= ScaleEpsilon
                && (transform.localScale - _baseScale).sqrMagnitude <= ScaleEpsilon)
                return;

            _baseScale = target;
            ApplyScale(duration, ease);
        }

        public RectTransform VisualRect => visual != null ? visual : (RectTransform)transform;

        private float RaiseScale()
        {
            if (trayHover) return CardAnimConfig.TrayHoverScale;

            return _picked ? CardAnimConfig.HandPickedScale : CardAnimConfig.HandHoverScale;
        }

        private float RaiseLift()
        {
            if (trayHover) return CardAnimConfig.TrayHoverLift;

            return _picked ? CardAnimConfig.HandPickedLift : CardAnimConfig.HandHoverLift;
        }

        private void ApplyRaise()
        {
            if (!hoverScale) return;

            _raised = mode == CardMode.InHand && (_pointerInside || _picked);

            CardLaneView lane = null;

            if (transform.parent != null)
                transform.parent.TryGetComponent(out lane);

            if (_raised)
                transform.SetAsLastSibling();
            else if (lane != null)
                lane.RestoreOrder();

            RaiseTo(
                _raised ? 1f : 0f,
                _raised ? RaiseLift() : 0f,
                _raised ? RaiseScale() - 1f : 0f);
        }

        private void RaiseTo(float t, float lift, float scale)
        {
            DOTween.Kill(_raiseKey);

            if (!Application.isPlaying)
            {
                _raiseT = t;
                _raiseLift = lift;
                _raiseScale = scale;
                ApplyVisual();
                return;
            }

            DOTween.To(() => _raiseT, value => _raiseT = value, t, CardAnimConfig.HoverDuration)
                .SetEase(CardAnimConfig.RowLayoutEase)
                .SetTarget(_raiseKey);

            DOTween.To(() => _raiseLift, value => _raiseLift = value, lift, CardAnimConfig.HoverDuration)
                .SetEase(CardAnimConfig.RowLayoutEase)
                .SetTarget(_raiseKey);

            DOTween.To(() => _raiseScale, value => _raiseScale = value, scale, CardAnimConfig.HoverDuration)
                .SetEase(CardAnimConfig.RowLayoutEase)
                .SetTarget(_raiseKey);
        }

        private void LateUpdate()
        {
            if (!hoverScale) return;

            ApplyVisual();
        }

        private void ApplyVisual()
        {
            var idle = _raiseT <= 0f && _raiseScale <= 0f && Mathf.Approximately(_raiseLift, 0f);

            if (idle && !_visualApplied) return;

            _visualApplied = !idle;

            var factor = 1f + _raiseScale;

            if (visual == null)
            {
                transform.localScale = _baseScale * factor;
                return;
            }

            var root = transform;
            var scale = Mathf.Approximately(_baseScale.x, 0f) ? 1f : _baseScale.x;
            var shift = Shift() / scale;

            visual.localScale = Vector3.one * factor;
            visual.localRotation = Quaternion.Euler(0f, 0f, -SignedAngle(root.localEulerAngles.z) * _raiseT);
            visual.localPosition = Quaternion.Inverse(root.localRotation) * new Vector3(0f, shift, 0f);

            ApplyFrame(shift, factor);
        }

        private float Shift()
        {
            CardLaneView lane = null;

            if (transform.parent != null)
                transform.parent.TryGetComponent(out lane);

            return lane != null ? lane.RaiseShift(transform, _raiseLift, _raiseT) : _raiseLift;
        }

        private void ApplyFrame(float lift, float factor)
        {
            if (_frame == null && !TryGetComponent(out _frame)) return;

            if (!_visualApplied)
            {
                _frame.raycastPadding = Vector4.zero;
                return;
            }

            var half = ((RectTransform)transform).rect.size * 0.5f;
            var grow = half * (factor - 1f);

            _frame.raycastPadding = new Vector4(-grow.x, 0f, -grow.x, -(grow.y + Mathf.Max(lift, 0f)));
        }

        private static float SignedAngle(float angle) => Mathf.Repeat(angle + 180f, 360f) - 180f;

        private void ApplyScale(float duration, Ease ease)
        {
            var rt = transform;
            var target = _baseScale;

            DOTween.Kill(_scaleKey);

            if (!Application.isPlaying || duration <= 0f)
            {
                rt.localScale = target;
                return;
            }

            DOTween.To(
                    () => rt.localScale,
                    value => rt.localScale = value,
                    target,
                    duration)
                .SetEase(ease)
                .SetTarget(_scaleKey);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (IsMouse(eventData))
                SetHovered(true);

            if (!CanPlayHover(eventData)) return;

            _hovered = true;
            PlayHover(hoverSoundId);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (IsMouse(eventData))
                SetHovered(false);

            if (!_hovered) return;

            _hovered = false;
            PlayHover(hoverOutSoundId);
        }

        public void PlayHoverSfx() => PlayHover(hoverSoundId);

        private bool CanPlayHover(PointerEventData eventData)
        {
            if (!hoverSfx) return false;

            return IsMouse(eventData);
        }

        private static bool IsMouse(PointerEventData eventData)
            => eventData is not ExtendedPointerEventData ext || ext.pointerType == UIPointerType.MouseOrPen;

        private void PlayHover(string soundId)
        {
            if (!hoverSfx) return;
            if (Time.unscaledTime - _lastHoverSoundTime < HoverSoundInterval) return;

            _lastHoverSoundTime = Time.unscaledTime;
            _audio?.Play(soundId);
        }

        private void OnDisable()
        {
            _hovered = false;
            _pointerInside = false;
            _picked = false;
            _dimmed = false;

            DOTween.Kill(_scaleKey);
            DOTween.Kill(_dimKey);
            DOTween.Kill(_raiseKey);

            _raiseT = 0f;
            _raiseLift = 0f;
            _raiseScale = 0f;

            if (_visualApplied)
            {
                _visualApplied = false;

                if (visual != null)
                {
                    visual.localScale = Vector3.one;
                    visual.localRotation = Quaternion.identity;
                    visual.localPosition = Vector3.zero;
                }

                if (_frame != null)
                    _frame.raycastPadding = Vector4.zero;
            }

            if (_raised)
            {
                _raised = false;
                transform.localScale = _baseScale;
            }

            if (_group != null)
                _group.alpha = 1f;
        }

        private void OnDestroy()
        {
            DOTween.Kill(_scaleKey);
            DOTween.Kill(_dimKey);
            _cardBindings.Dispose();
        }

        public void HitReact()
        {
            if (!Application.isPlaying || cardImage == null) return;

            var shaken = cardImage.transform;
            shaken.DOKill(true);
            shaken.DOShakePosition(
                CardAnimConfig.HitReactDuration,
                CardAnimConfig.HitShakeStrength);
        }

        public void TugReact(Vector2 direction)
        {
            if (!Application.isPlaying || cardImage == null) return;
            if (direction.sqrMagnitude < 0.0001f) return;

            var pulled = cardImage.transform;
            pulled.DOKill(true);
            pulled.DOPunchPosition(
                direction.normalized * CardAnimConfig.LureTugStrength,
                CardAnimConfig.LureTugDuration,
                CardAnimConfig.LureTugVibrato,
                0.6f);
        }

        private void PlayBuff()
        {
            if (!Application.isPlaying || _vfx == null || mode != CardMode.OnBoard) return;

            _vfx.PlayFitted(BuffVfxId, (RectTransform)transform);
        }

        private void PowerPop()
        {
            if (!Application.isPlaying || powerText == null) return;
            if (mode == CardMode.InGraveyard) return;

            var popped = powerText.transform;
            popped.DOKill(true);
            popped.localScale = Vector3.one;
            popped.DOPunchScale(
                Vector3.one * (CardAnimConfig.PowerPopScale - 1f),
                CardAnimConfig.PowerPopDuration,
                1,
                0.5f);
        }
    }
}