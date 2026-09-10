using DG.Tweening;
using IsntGwent.Scripts.Cards.Definitions;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Cards.UI
{
    public enum SlotHighlight { None, Free, Zone, Placement, Damage, Support }

    [RequireComponent(typeof(Image))]
    public class SlotView : MonoBehaviour
    {
        private static readonly Color IdleFrame = new(0.84f, 0.75f, 0.59f, 0.85f);

        private const float FrameTintAlpha = 0.55f;
        private const float IdleIconAlpha = 0.18f;
        private const float FreeIconAlpha = 0.4f;
        private const float ZoneIconAlpha = 0.35f;

        private const float FreeGlowAlpha = 0.34f;
        private const float ZoneGlowAlpha = 0.55f;
        private const float TargetGlowAlpha = 0.8f;
        private const float PlacementGlowAlpha = 1f;
        private const float PulseFloor = 0.55f;

        private static Sprite _meleeSprite;
        private static Sprite _rangedSprite;
        private static Sprite _frameSprite;

        public BoardRowView Row { get; private set; }
        public int Index { get; private set; }

        private Image _hitArea;
        private Image _glow;
        private Image _frame;
        private Image _icon;

        private RectTransform _glowRt;
        private RectTransform _frameRt;
        private RectTransform _iconRt;

        public void Setup(BoardRowView row, int index, RowType rowType)
        {
            Row = row;
            Index = index;

            _hitArea = GetComponent<Image>();
            _hitArea.color = Color.clear;
            _hitArea.raycastTarget = true;

            _glow = GlowSprite.CreateInner(transform, "Glow");
            GlowSprite.SetWidth(_glow, HighlightPalette.SlotGlowWidth);

            _frameSprite ??= Resources.Load<Sprite>("Sprites/UI/ui_slot");
            _frame = CreateChild("Frame", IdleFrame, _frameSprite);
            _frame.type = Image.Type.Tiled;
            _icon = CreateChild("Icon", new Color(1f, 1f, 1f, IdleIconAlpha), RowSprite(rowType));

            _glowRt = (RectTransform)_glow.transform;
            _frameRt = (RectTransform)_frame.transform;
            _iconRt = (RectTransform)_icon.transform;

            _icon.preserveAspect = true;

            _icon.gameObject.SetActive(_icon.sprite != null);

            SetHighlight(SlotHighlight.None);
        }

        public void SetSize(Vector2 outer, Vector2 frame, float iconScale)
        {
            var rt = (RectTransform)transform;
            rt.sizeDelta = outer;

            var inner = new Vector2(
                Mathf.Max(frame.x, 1f),
                Mathf.Max(frame.y, 1f));

            if (_frameRt != null)
                _frameRt.sizeDelta = inner;

            if (_glowRt != null)
                _glowRt.sizeDelta = inner + Vector2.one * (HighlightPalette.SlotGlowBleed * 2f);

            var iconSize = Vector2.one * Mathf.Max(inner.x * iconScale, 1f);

            if (_iconRt != null)
                _iconRt.sizeDelta = iconSize;
        }

        public void SetHighlight(SlotHighlight state)
        {
            if (_frame == null) return;

            var light = state switch
            {
                SlotHighlight.Free => HighlightPalette.LightChoosable,
                SlotHighlight.Zone => HighlightPalette.LightChoosable,
                SlotHighlight.Placement => HighlightPalette.LightPlacement,
                SlotHighlight.Damage => HighlightPalette.LightDamage,
                SlotHighlight.Support => HighlightPalette.LightSupport,
                _ => HighlightPalette.Hidden,
            };

            var glowAlpha = state switch
            {
                SlotHighlight.Free => FreeGlowAlpha,
                SlotHighlight.Zone => ZoneGlowAlpha,
                SlotHighlight.Placement => PlacementGlowAlpha,
                SlotHighlight.Damage => TargetGlowAlpha,
                SlotHighlight.Support => TargetGlowAlpha,
                _ => 0f,
            };

            var glowColor = HighlightPalette.WithAlpha(light, glowAlpha);

            var frameColor = state == SlotHighlight.None
                ? IdleFrame
                : HighlightPalette.WithAlpha(light, FrameTintAlpha);

            var iconAlpha = state switch
            {
                SlotHighlight.Free => FreeIconAlpha,
                SlotHighlight.None => IdleIconAlpha,
                _ => ZoneIconAlpha,
            };

            Fade(_glow, glowColor);
            Fade(_frame, frameColor);
            Fade(_icon, new Color(1f, 1f, 1f, iconAlpha));

            Pulse(glowAlpha);
        }

        private void Pulse(float glowAlpha)
        {
            if (_glow == null || glowAlpha <= 0f || !Application.isPlaying) return;

            _glow
                .DOFade(glowAlpha * PulseFloor, CardAnimConfig.SlotGlowPulseDuration)
                .SetDelay(CardAnimConfig.SlotHighlightDuration + Index * CardAnimConfig.SlotGlowPulseStagger)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private static void Fade(Image image, Color target)
        {
            if (image == null) return;

            image.DOKill();

            if (!Application.isPlaying)
            {
                image.color = target;
                return;
            }

            image.DOColor(target, CardAnimConfig.SlotHighlightDuration);
        }

        private Image CreateChild(string childName, Color color, Sprite sprite)
        {
            var go = new GameObject(childName, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(transform, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;

            return image;
        }

        private static Sprite RowSprite(RowType row)
        {
            switch (row)
            {
                case RowType.Melee:
                    _meleeSprite ??= Resources.Load<Sprite>("Sprites/sword_icon");
                    return _meleeSprite;
                case RowType.Ranged:
                    _rangedSprite ??= Resources.Load<Sprite>("Sprites/bow_icon");
                    return _rangedSprite;
                default:
                    return null;
            }
        }
    }
}
