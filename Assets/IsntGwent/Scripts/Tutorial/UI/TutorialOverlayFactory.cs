using System.Linq;
using IsntGwent.Scripts.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IsntGwent.Scripts.Tutorial.UI
{
    public static class TutorialOverlayFactory
    {
        private const string WindowSprite = "Sprites/UI/ui_window";

        private const float PanelWidth = 1200f;
        private const float FontSize = 52f;

        private static readonly Color TextColor = new(0.941f, 0.886f, 0.776f, 1f);

        public static TutorialOverlayView Create()
        {
            var canvas = RootCanvas();

            if (canvas == null)
            {
                Log.Error(LogTag.Tutorial, "no screen canvas in the scene, the overlay has nowhere to live");

                return null;
            }

            Log.Info(LogTag.Tutorial, $"overlay canvas: {canvas.name}");

            var host = NewRect("---Tutorial---", canvas.transform);
            Stretch(host);

            var overlay = NewRect("Overlay", host);
            Stretch(overlay);

            var group = overlay.gameObject.AddComponent<CanvasGroup>();

            var blocker = TapArea(overlay, out var catcher);
            var panel = Panel(overlay, out var text);

            var view = host.gameObject.AddComponent<TutorialOverlayView>();
            view.Bind(overlay, group, blocker, panel, text, catcher);

            return view;
        }

        public static Canvas RootCanvas()
        {
            return Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace)
                .OrderByDescending(c => c.GetComponentsInChildren<Graphic>(true).Length)
                .FirstOrDefault();
        }

        private static RectTransform TapArea(Transform parent, out Button catcher)
        {
            var rect = NewRect("TapArea", parent);
            Stretch(rect);

            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;

            catcher = rect.gameObject.AddComponent<Button>();
            catcher.transition = Selectable.Transition.None;
            catcher.targetGraphic = image;

            return rect;
        }

        private static RectTransform Panel(Transform parent, out TextMeshProUGUI text)
        {
            var rect = NewRect("TextPanel", parent);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(PanelWidth, 120f);

            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = TutorialOverlayView.PanelSortingOrder;

            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>(WindowSprite);
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;

            var layout = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 24, 24);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textRect = NewRect("Text", rect);
            text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = SceneFont();
            text.fontSize = FontSize;
            text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;

            return rect;
        }

        private static TMP_FontAsset SceneFont()
        {
            var fonts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(t => t.font != null)
                .GroupBy(t => t.font)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .ToList();

            var fallback = TMP_Settings.defaultFontAsset;

            return fonts.FirstOrDefault(f => f != fallback) ?? fonts.FirstOrDefault();
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;

            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;

            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
