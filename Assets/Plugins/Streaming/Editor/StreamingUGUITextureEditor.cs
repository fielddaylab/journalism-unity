using UnityEngine;
using UnityEditor;

namespace StreamingAssets.Editor {
    [CustomEditor(typeof(StreamingUGUITexture)), CanEditMultipleObjects]
    public sealed class StreamingUGUITextureEditor : UnityEditor.Editor {

        private SerializedProperty m_PathProperty;
        private SerializedProperty m_UVRectProperty;
        private SerializedProperty m_AutoSizeProperty;

        private void OnEnable() {
            m_PathProperty = serializedObject.FindProperty("m_Path");
            m_UVRectProperty = serializedObject.FindProperty("m_UVRect");
            m_AutoSizeProperty = serializedObject.FindProperty("m_AutoSize");
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUILayout.PropertyField(m_PathProperty);
            EditorGUILayout.PropertyField(m_UVRectProperty);
            EditorGUILayout.PropertyField(m_AutoSizeProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}