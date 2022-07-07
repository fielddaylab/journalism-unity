using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using System.Globalization;
using System;

namespace FDLocalization.Editor {
    [CustomEditor(typeof(LocText)), CanEditMultipleObjects]
    public sealed class LocTextEditor : UnityEditor.Editor {
        private SerializedProperty m_DefaultIdProperty;
        private SerializedProperty m_PrefixProperty;
        private SerializedProperty m_PostfixProperty;
        
        private void OnEnable() {
            m_DefaultIdProperty = serializedObject.FindProperty("m_DefaultId");
            m_PrefixProperty = serializedObject.FindProperty("m_Prefix");
            m_PostfixProperty = serializedObject.FindProperty("m_Postfix");
        }

        public override void OnInspectorGUI() {
            serializedObject.UpdateIfRequiredOrScript();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_DefaultIdProperty);
            EditorGUILayout.PropertyField(m_PrefixProperty);
            EditorGUILayout.PropertyField(m_PostfixProperty);
            if (EditorGUI.EndChangeCheck()) {
                serializedObject.ApplyModifiedProperties();
                foreach(LocText text in targets) {
                    string val;
                    if (EditorLocDB.TryLookup(text.m_DefaultId.ToDebugString(), out val)) {
                        Undo.RecordObject(text.Graphic, "Set Text");
                        text.InternalSetText(val);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}