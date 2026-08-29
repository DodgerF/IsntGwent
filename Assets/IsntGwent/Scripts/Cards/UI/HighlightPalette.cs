using UnityEngine;

namespace IsntGwent.Scripts.Cards.UI
{
    public static class HighlightPalette
    {
        public static readonly Color Choosable = new(0.227f, 0.271f, 0.314f, 1f);
        public static readonly Color Placement = new(0.431f, 0.353f, 0.173f, 1f);
        public static readonly Color Damage = new(0.420f, 0.125f, 0.094f, 1f);
        public static readonly Color Support = new(0.184f, 0.290f, 0.235f, 1f);
        public static readonly Color Hidden = new(0f, 0f, 0f, 0f);

        public static readonly Color LightChoosable = new(0.561f, 0.663f, 0.729f, 1f);
        public static readonly Color LightPlacement = new(0.910f, 0.749f, 0.443f, 1f);
        public static readonly Color LightDamage = new(0.839f, 0.353f, 0.239f, 1f);
        public static readonly Color LightSupport = new(0.471f, 0.753f, 0.553f, 1f);

        public const float SlotGlowWidth = 18f;
        public const float SlotGlowBleed = 4f;
        public const float RowGlowWidth = 34f;
        public const float CardGlowWidth = 22f;

        private const float SelectedBoost = 1.9f;

        public static Color Selected => Brighten(Choosable, SelectedBoost);

        public static Color Brighten(Color color, float factor)
            => new(
                Mathf.Clamp01(color.r * factor),
                Mathf.Clamp01(color.g * factor),
                Mathf.Clamp01(color.b * factor),
                color.a);

        public static Color WithAlpha(Color color, float alpha)
            => new(color.r, color.g, color.b, alpha);
    }
}
