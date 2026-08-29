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

        public bool hoverSfx = true;
        public bool hoverScale;
        public string hoverSoundId = "ui_hover";
        public string hoverOutSoundId = "ui_hover_out";

        [Inject] private readonly AudioService _audio;
        [InjectOptional] private readonly VfxService _vfx;

        private readonly CompositeDisposable _cardBindings = new();
        private readonly object _scaleKey = new();
        private readonly object _dimKey = new();

        private bool _hovered;
        private bool _pointerInside;
        private bool _picked;
        private bool _dimmed;
        private bool _raised;
        private Vector3 _baseScale = Vector3.one;
        private CanvasGroup _group;
        private Image _glow;

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
                var basePower = unitInstance.UnitDefinition.Power;

                powerText.gameObject.SetActive(true);
                if (powerBack != null) powerBack.SetActive(true);

                unitInstance.CurrentPower
                    .Subscribe(power =>
                    {
                        powerText.text = power.ToString();
                        powerText.color = PowerColor(power, basePower);
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

            _glow = GlowSprite.Create(transform, "CardGlow");
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
                && (transform.localScale - _baseScale * RaiseFactor()).sqrMagnitude <= ScaleEpsilon)
                return;

            _baseScale = target;
            ApplyScale(duration, ease);
        }

        private float RaiseFactor()
        {
            if (!_raised) return 1f;

            return _picked ? CardAnimConfig.HandPickedScale : CardAnimConfig.HandHoverScale;
        }

        private float RaiseLift()
            => _picked ? CardAnimConfig.HandPickedLift : CardAnimConfig.HandHoverLift;

        private void ApplyRaise()
        {
            if (!hoverScale) return;

            _raised = mode == CardMode.InHand && (_pointerInside || _picked);

            CardLaneView lane = null;

            if (transform.parent != null)
                transform.parent.TryGetComponent(out lane);

            if (lane != null)
                lane.SetRaised(transform, _raised, RaiseLift());

            if (_raised)
                transform.SetAsLastSibling();
            else if (lane != null)
                lane.RestoreOrder();

            ApplyScale(CardAnimConfig.HoverDuration, CardAnimConfig.RowLayoutEase);
        }

        private void ApplyScale(float duration, Ease ease)
        {
            var rt = transform;
            var target = _baseScale * RaiseFactor();

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