using IsntGwent.Scripts.UI;
using UnityEditor;

namespace Editor
{
    [CustomEditor(typeof(SfxButton), true)]
    [CanEditMultipleObjects]
    public class SfxButtonEditor : UnityEditor.UI.ButtonEditor
    {
        private SerializedProperty _clickSoundId;
        private SerializedProperty _hoverSoundId;

        protected override void OnEnable()
        {
            base.OnEnable();

            _clickSoundId = serializedObject.FindProperty("clickSoundId");
            _hoverSoundId = serializedObject.FindProperty("hoverSoundId");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            serializedObject.Update();
            EditorGUILayout.PropertyField(_clickSoundId);
            EditorGUILayout.PropertyField(_hoverSoundId);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
