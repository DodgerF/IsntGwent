using IsntGwent.Scripts.UI;
using UnityEditor;
using UnityEngine.UI;

namespace Editor
{
    public static class SfxButtonScriptSwap
    {
        private static MonoScript _script;

        public static MonoScript Script
        {
            get
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

        public static bool Convert(Button button)
        {
            if (button == null || button is SfxButton) return false;
            if (Script == null) return false;

            var serialized = new SerializedObject(button);
            serialized.FindProperty("m_Script").objectReferenceValue = Script;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}
