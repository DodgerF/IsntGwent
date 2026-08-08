using IsntGwent.Scripts.Decks.UI;
using IsntGwent.Scripts.Lobby.UI.Views;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SfxButton = IsntGwent.Scripts.UI.SfxButton;

namespace Editor
{
    public static class DeckListSetup
    {
        private const string TilePath = "Assets/IsntGwent/Prefabs/NewDeckTile.prefab";
        private const string DeckViewPath = "Assets/IsntGwent/Prefabs/DeckView.prefab";

        private const string SelectedFrameName = "SelectedFrame";

        private static readonly Vector2 TileSize = new(200, 300);
        private static readonly Color OutlineColor = new(1f, 1f, 1f, 0.55f);
        private static readonly Color SelectedColor = new(1f, 0.85f, 0.3f, 1f);

        [MenuItem("Tools/IsntGwent/Deck List/Create New Deck Tile Prefab")]
        public static void CreateTilePrefab()
        {
            var root = new GameObject("NewDeckTile", typeof(RectTransform), typeof(Image), typeof(SfxButton),
                typeof(NewDeckTileView));

            var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = TileSize;

            var image = root.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            var button = root.GetComponent<SfxButton>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            AddBorder(rect, "Outline", OutlineColor, 3f);
            AddPlus(rect);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, TilePath);
            Object.DestroyImmediate(root);

            Debug.Log(prefab != null ? "Created " + TilePath : "Failed to create " + TilePath);
        }

        [MenuItem("Tools/IsntGwent/Deck List/Add Selected Frame To DeckView")]
        public static void AddSelectedFrame()
        {
            var root = PrefabUtility.LoadPrefabContents(DeckViewPath);
            if (root == null)
            {
                Debug.LogError("Prefab not found: " + DeckViewPath);
                return;
            }

            var view = root.GetComponent<DeckSelectionView>();
            if (view == null)
            {
                Debug.LogError("No DeckSelectionView on " + DeckViewPath);
                PrefabUtility.UnloadPrefabContents(root);
                return;
            }

            var existing = root.transform.Find(SelectedFrameName);
            var frame = existing != null
                ? (RectTransform)existing
                : AddBorder((RectTransform)root.transform, SelectedFrameName, SelectedColor, 5f);

            frame.SetAsLastSibling();
            frame.gameObject.SetActive(false);

            var serialized = new SerializedObject(view);
            serialized.FindProperty("selectedFrame").objectReferenceValue = frame.gameObject;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, DeckViewPath);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log("SelectedFrame added to " + DeckViewPath);
        }

        [MenuItem("Tools/IsntGwent/Deck List/Rebuild Deck List Scroll")]
        public static void RebuildScroll()
        {
            var list = Object.FindFirstObjectByType<DeckListViewBase>(FindObjectsInactive.Include);
            if (list == null)
            {
                Debug.LogError("No deck list in the open scene. Open Menu.unity or DeckBuilder.unity first.");
                return;
            }

            if (list.parent == null)
            {
                Debug.LogError("Deck list has no parent transform assigned.");
                return;
            }

            var container = (RectTransform)list.parent;

            if (container.GetComponent<ScrollRect>() != null)
            {
                Debug.Log("Deck list is already a scroll view, nothing to do.");
                return;
            }

            var oldLayout = container.GetComponent<HorizontalLayoutGroup>();
            if (oldLayout != null) Object.DestroyImmediate(oldLayout, true);

            if (container.sizeDelta.y < TileSize.y)
                container.sizeDelta = new Vector2(Mathf.Max(container.sizeDelta.x, 1100f), TileSize.y + 40f);

            var viewport = NewRect("Viewport", container);
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.pivot = new Vector2(0.5f, 0.5f);
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewport.gameObject.AddComponent<RectMask2D>();

            var content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 0f);
            content.anchorMax = new Vector2(0f, 1f);
            content.pivot = new Vector2(0f, 0.5f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var layout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 50f;
            layout.padding = new RectOffset(20, 20, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scroll = container.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.1f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.scrollSensitivity = 20f;
            scroll.viewport = viewport;
            scroll.content = content;

            var tile = AssetDatabase.LoadAssetAtPath<GameObject>(TilePath);

            var serialized = new SerializedObject(list);
            serialized.FindProperty("parent").objectReferenceValue = content;

            if (tile != null)
                serialized.FindProperty("newDeckTilePrefab").objectReferenceValue = tile.GetComponent<NewDeckTileView>();

            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(list.gameObject.scene);

            Debug.Log("Rebuilt deck list scroll on " + container.name + " (" + list.gameObject.scene.name + "). Save the scene.");
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;

            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;

            return rect;
        }

        private static RectTransform AddBorder(RectTransform parent, string name, Color color, float thickness)
        {
            var root = NewRect(name, parent);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            AddBar(root, "Top", color, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, thickness));
            AddBar(root, "Bottom", color, Vector2.zero, new Vector2(1f, 0f), new Vector2(0f, thickness));
            AddBar(root, "Left", color, Vector2.zero, new Vector2(0f, 1f), new Vector2(thickness, 0f));
            AddBar(root, "Right", color, new Vector2(1f, 0f), Vector2.one, new Vector2(thickness, 0f));

            return root;
        }

        private static void AddBar(RectTransform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 size)
        {
            var rect = NewRect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void AddPlus(RectTransform parent)
        {
            var rect = NewRect("Plus (TMP)", parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = "+";
            text.fontSize = 120f;
            text.color = OutlineColor;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }
    }
}
