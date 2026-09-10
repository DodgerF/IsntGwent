using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Cards.UI
{
    public static class GlowSprite
    {
        private const int TextureSize = 96;
        private const int TextureBorder = 40;
        private const string MaterialPath = "Shaders/M_UI_GlowMax";

        private static Sprite _shared;
        private static Sprite _sharedInner;
        private static Material _material;

        public static Sprite Shared => _shared != null ? _shared : _shared = Build(false);

        public static Sprite SharedInner => _sharedInner != null ? _sharedInner : _sharedInner = Build(true);

        public static Material Material
            => _material != null ? _material : _material = Resources.Load<Material>(MaterialPath);

        public static Image Create(Transform parent, string name, Material material = null)
            => Create(parent, name, Shared, material);

        public static Image CreateInner(Transform parent, string name, Material material = null)
            => Create(parent, name, SharedInner, material);

        public static void SetWidth(Image image, float glowWidth)
        {
            if (image == null) return;

            image.pixelsPerUnitMultiplier = TextureBorder / Mathf.Max(glowWidth, 1f);
        }

        private static Image Create(Transform parent, string name, Sprite sprite, Material material)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;

            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.sprite = sprite;
            image.material = material != null ? material : Material;
            image.type = Image.Type.Sliced;
            image.color = HighlightPalette.Hidden;

            return image;
        }

        private static Sprite Build(bool inner)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[TextureSize * TextureSize];

            for (var y = 0; y < TextureSize; y++)
            for (var x = 0; x < TextureSize; x++)
            {
                var edge = Mathf.Min(Mathf.Min(x, TextureSize - 1 - x), Mathf.Min(y, TextureSize - 1 - y));
                var t = Mathf.Clamp01(edge / (float)TextureBorder);
                var alpha = inner ? (1f - t) * (1f - t) : t * t;

                pixels[y * TextureSize + x] = new Color(1f, 1f, 1f, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var border = new Vector4(TextureBorder, TextureBorder, TextureBorder, TextureBorder);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                border);

            sprite.hideFlags = HideFlags.HideAndDontSave;

            return sprite;
        }
    }
}
