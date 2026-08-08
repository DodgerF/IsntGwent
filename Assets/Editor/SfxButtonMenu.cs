using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Editor
{
    public static class SfxButtonMenu
    {
        [MenuItem("GameObject/UI (Canvas)/Sfx Button", false, 2032)]
        public static void Create(MenuCommand menuCommand)
        {
            TMPro_CreateObjectMenu.AddButton(menuCommand);

            var go = Selection.activeGameObject;
            if (go == null) return;

            var button = go.GetComponent<Button>();
            if (button == null || !SfxButtonScriptSwap.Convert(button)) return;

            go.name = "SfxButton";
            EditorUtility.SetDirty(go);
        }
    }
}
