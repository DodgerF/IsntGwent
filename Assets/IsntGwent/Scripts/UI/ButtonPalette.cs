using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.UI
{
    public static class ButtonPalette
    {
        public const float DisabledContentAlpha = 0.35f;

        public static readonly ColorBlock Default = new()
        {
            normalColor = new Color(1f, 1f, 1f, 1f),
            highlightedColor = new Color(1f, 0.87f, 0.62f, 1f),
            pressedColor = new Color(0.7882353f, 0.6313726f, 0.34901962f, 1f),
            selectedColor = new Color(1f, 1f, 1f, 1f),
            disabledColor = new Color(0.38f, 0.36f, 0.33f, 1f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f,
        };
    }
}
