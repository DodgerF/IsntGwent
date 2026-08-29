using IsntGwent.Scripts.UI;
using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor
{
    public static class SfxButtonMenu
    {
        private static MonoScript _script;

        [MenuItem("GameObject/UI (Canvas)/Sfx Button", false, 2032)]
        public static void Create(MenuCommand menuCommand)
        {
            TMPro_CreateObjectMenu.AddButton(menuCommand);

            var go = Selection.activeGameObject;
            if (go == null) return;

            var button = go.GetComponent<Button>();
            if (button == null || !Convert(button)) return;

            go.name = "SfxButton";
            EditorUtility.SetDirty(go);
        }

        private static bool Convert(Button button)
        {
            if (button == null || button is SfxButton) return false;

            var script = Script();
            if (script == null) return false;

            var serialized = new SerializedObject(button);
            serialized.FindProperty("m_Script").objectReferenceValue = script;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return true;
        }

        private static MonoScript Script()
        {
            if (_script != null) return _script;

            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript SfxButton"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var candidate = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (candidate == null || candidate.GetClass() != typeof(SfxButton)) continue;

                _script = candidate;
                return _script;
            }

            return null;
        }
    }
}
