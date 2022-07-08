using System;
using System.Collections.Generic;
using System.Globalization;
using BeauUtil;
using BeauUtil.Editor;
using UnityEditor;
using UnityEngine;

namespace FDLocalization.Editor {
    [CustomPropertyDrawer(typeof(LocId))]
    public sealed class LocIdEditor : PropertyDrawer {

        private const float TextIconDisplayWidth = 64;
        private const int MaxSearchLines = 12;

        private class PropState {
            public string Text;
            public string LastKnownKey;
            public bool Overwrite = true;
        }

        private readonly Dictionary<uint, PropState> m_States = new Dictionary<uint, PropState>();

        [NonSerialized] static private GUIStyle s_NullIconStyle;
        [NonSerialized] static private GUIStyle s_FoundIconStyle;
        [NonSerialized] static private GUIStyle s_OverwriteIconStyle;
        [NonSerialized] static private GUIStyle s_MissingIconStyle;
        [NonSerialized] static private GUIStyle s_HintIconStyle;

        [NonSerialized] static private GUIContent s_MissingContent;
        [NonSerialized] static private GUIContent s_ValidContent;
        [NonSerialized] static private GUIContent s_OverwriteContent;
        [NonSerialized] static private GUIContent s_HintContent;

        [NonSerialized] static private GUIStyle s_SearchBoxStyle;
        [NonSerialized] static private GUIStyle s_TextAreaStyle;

        [NonSerialized] private readonly List<string> m_SearchResults = new List<string>();
        [NonSerialized] private int m_SearchScroll = 0;

        static private void InitializeStyles() {
            if (s_NullIconStyle == null) {
                s_NullIconStyle = new GUIStyle(EditorStyles.label);
                s_NullIconStyle.normal.textColor = Color.gray;
                s_NullIconStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (s_FoundIconStyle == null) {
                s_FoundIconStyle = new GUIStyle(EditorStyles.label);
                s_FoundIconStyle.normal.textColor = Color.green;
                s_FoundIconStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (s_OverwriteIconStyle == null) {
                s_OverwriteIconStyle = new GUIStyle(EditorStyles.miniButton);
                s_OverwriteIconStyle.normal.textColor = Color.green;
                s_OverwriteIconStyle.alignment = TextAnchor.MiddleCenter; 
            }

            if (s_MissingIconStyle == null) {
                s_MissingIconStyle = new GUIStyle(EditorStyles.miniButton);
                s_MissingIconStyle.normal.textColor = Color.yellow;
                s_MissingIconStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (s_HintIconStyle == null) {
                s_HintIconStyle = new GUIStyle(EditorStyles.miniButton);
                s_HintIconStyle.normal.textColor = Color.cyan;
                s_HintIconStyle.alignment = TextAnchor.MiddleCenter;
            }

            if (s_SearchBoxStyle == null) {
                s_SearchBoxStyle = new GUIStyle(EditorStyles.helpBox);
                s_SearchBoxStyle.normal.background = Texture2D.whiteTexture;
            }

            if (s_TextAreaStyle == null) {
                s_TextAreaStyle = new GUIStyle(EditorStyles.textArea);
                s_TextAreaStyle.wordWrap = true;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            InitializeStyles();

            LocIdHintAttribute hint;
            if (property.serializedObject.isEditingMultipleObjects) {
                hint = null;
            } else {
                hint =  Reflect.GetAttribute<LocIdHintAttribute>(fieldInfo);
            }

            Rect firstLine = position;
            firstLine.height = EditorGUIUtility.singleLineHeight;

            label = EditorGUI.BeginProperty(position, label, property);
            Rect propRect = firstLine;
            propRect.width -= TextIconDisplayWidth + 4;

            Event oldCurrent = new Event(Event.current);

            uint stateKey = new StringHash32(property.propertyPath).HashValue;
            PropState state;
            if (!m_States.TryGetValue(stateKey, out state)) {
                state = new PropState();
                m_States.Add(stateKey, state);
            }
            
            EditorGUI.BeginChangeCheck();
            var stringProp = property.FindPropertyRelative("m_Source");
            var hashProp = property.FindPropertyRelative("m_HashValue");

            if (!stringProp.hasMultipleDifferentValues && stringProp.stringValue != state.LastKnownKey) {
                state.Overwrite = true;
                state.LastKnownKey = stringProp.stringValue;
            }

            int nextControlId = GUIUtility.GetControlID(FocusType.Keyboard) + 1;
            EditorGUI.showMixedValue = stringProp.hasMultipleDifferentValues;
            string newString = EditorGUI.TextField(propRect, label, stringProp.stringValue);
            EditorGUI.showMixedValue = false;
            bool bSelected = GUIUtility.keyboardControl == nextControlId;

            if (UnityEditor.EditorGUI.EndChangeCheck()) {
                stringProp.stringValue = newString;
                state.Overwrite = true;
                state.LastKnownKey = stringProp.stringValue;
                hashProp.longValue = new StringHash32(stringProp.stringValue).HashValue;
            }

            Rect statusRect = new Rect(propRect.xMax + 4, propRect.y, TextIconDisplayWidth, propRect.height);

            Rect searchResultsBox = EditorGUI.IndentedRect(firstLine);
            searchResultsBox.height = 0;
            searchResultsBox.y -= 4;
            searchResultsBox.width -= TextIconDisplayWidth + 4;

            Rect textBox = EditorGUI.IndentedRect(position);
            textBox.height -= 22;
            textBox.y += 20;

            float textBoxWidthToReduce = 24;
            textBox.x += textBoxWidthToReduce;
            textBox.width -= textBoxWidthToReduce;

            string key = stringProp.stringValue;
            key = stringProp.stringValue;
            string foundContent = null;
            bool bFound = !string.IsNullOrEmpty(key) && EditorLocDB.TryLookup(key, out foundContent);
            if (state.Overwrite && bFound) {
                state.Text = foundContent;
                state.Overwrite = false;
            }

            using (GUIScopes.IndentLevelScope.SetIndent(0)) {
                if (!string.IsNullOrEmpty(key)) {
                    state.Text = EditorGUI.TextArea(textBox, state.Text, s_TextAreaStyle);
                }
                RenderSearchAndStatus(property, stringProp, hashProp, statusRect, searchResultsBox, key, foundContent, state, hint, bFound, bSelected, oldCurrent);
            }

            UnityEditor.EditorGUI.EndProperty();
        }

        private void RenderSearchAndStatus(SerializedProperty parentProp, SerializedProperty stringProp, SerializedProperty hashProp, Rect statusPosition, Rect searchPosition, string key, string originalText, PropState state, LocIdHintAttribute hint, bool found, bool selected, Event evt) {
            if (stringProp.hasMultipleDifferentValues) {
                EditorGUI.LabelField(statusPosition, "---", s_NullIconStyle);
                return;
            }
            if (string.IsNullOrEmpty(key)) {
                if (hint != null) {
                    var hintContent = s_HintContent ?? (s_HintContent = new GUIContent("Suggest?"));
                    if (GUI.Button(statusPosition, hintContent, s_HintIconStyle)) {
                        string hintKey = hint.GetHint(fieldInfo, parentProp.FindOwner(), parentProp.serializedObject.targetObject);
                        if (!string.IsNullOrEmpty(hintKey)) {
                            state.LastKnownKey = hintKey;
                            stringProp.stringValue = hintKey;
                            hashProp.longValue = new StringHash32(hintKey).HashValue;
                            state.Overwrite = true;
                        }
                    }
                } else {
                    EditorGUI.LabelField(statusPosition, "Null", s_NullIconStyle);
                }
                return;
            }


            if (found) {
                if (originalText == state.Text) {
                    var validContent = s_ValidContent ?? (s_ValidContent = new GUIContent("Good"));
                    EditorGUI.LabelField(statusPosition, validContent, s_FoundIconStyle);
                } else {
                    var updateContent = s_OverwriteContent ?? (s_OverwriteContent = new GUIContent("Update?"));
                    if (GUI.Button(statusPosition, updateContent, s_OverwriteIconStyle)) {
                        EditorLocDB.TrySet(key, state.Text);
                    }
                }

                return;
            }

            var guiContent = s_MissingContent ?? (s_MissingContent = new GUIContent("Create?"));
            if (GUI.Button(statusPosition, guiContent, s_MissingIconStyle)) {
                EditorLocDB.TrySet(key, state.Text);
                EditorUtility.SetDirty(stringProp.serializedObject.targetObject);
            }

            if (selected && evt.type == EventType.KeyDown) {
                if (evt.keyCode == KeyCode.PageUp) {
                    m_SearchScroll -= 1;
                    EditorUtility.SetDirty(stringProp.serializedObject.targetObject);
                } else if (evt.keyCode == KeyCode.PageDown) {
                    m_SearchScroll += 1;
                    EditorUtility.SetDirty(stringProp.serializedObject.targetObject);
                } else if (evt.keyCode == KeyCode.Space && m_SearchResults.Count == 1) {
                    string onlyResult = m_SearchResults[0];
                    state.LastKnownKey = onlyResult;
                    stringProp.stringValue = onlyResult;
                    hashProp.longValue = new StringHash32(onlyResult).HashValue;
                    state.Overwrite = true;
                    GUIUtility.keyboardControl = 0;
                }
            }

            if (selected && evt.type == EventType.Repaint) {
                PaintSearchBox(key, searchPosition);
            }
        }

        private void PaintSearchBox(string key, Rect searchPosition) {
            m_SearchResults.Clear();
            int resultCount = EditorLocDB.Search(key, m_SearchResults);
            int resultsToDisplay = Math.Min(MaxSearchLines, resultCount);

            m_SearchScroll = Mathf.Clamp(m_SearchScroll, 0, resultCount - resultsToDisplay);

            searchPosition.height = ((resultsToDisplay + 1) * EditorGUIUtility.singleLineHeight) + resultsToDisplay * 2;
            searchPosition.y -= searchPosition.height + 2;

            Rect searchBackgroundBox = searchPosition;
            searchBackgroundBox.x -= 2;
            searchBackgroundBox.y -= 2;
            searchBackgroundBox.width += 4;
            searchBackgroundBox.height += 4;

            using (new GUIScopes.ColorScope(Color.black.WithAlpha(0.8f))) {
                GUI.Box(searchBackgroundBox, " ", s_SearchBoxStyle);
            }

            float lineInterval = EditorGUIUtility.singleLineHeight + 2;

            Rect textRect = searchPosition;
            textRect.height = EditorGUIUtility.singleLineHeight;
            if (resultsToDisplay == 0) {
                GUI.Label(textRect, "No similar entries", s_NullIconStyle);
            } else {
                if (resultCount > MaxSearchLines)
                    GUI.Label(textRect, string.Format("{0} similar entries\t[PgUp/PgDn to scroll]", resultCount), s_NullIconStyle);
                else
                    GUI.Label(textRect, string.Format("{0} similar entries", resultCount), s_NullIconStyle);
                textRect.y += lineInterval;

                textRect.x += 4;
                textRect.width -= 4;

                for (int i = 0; i < resultsToDisplay; i++) {
                    GUI.Label(textRect, m_SearchResults[m_SearchScroll + i]);
                    textRect.y += lineInterval;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            var hashProp = property.FindPropertyRelative("m_HashValue");
            if (hashProp.longValue == 0) {
                return EditorGUIUtility.singleLineHeight;
            }
            return EditorGUIUtility.singleLineHeight + 4 + 18f + 13 + 13;
        }
    }
}