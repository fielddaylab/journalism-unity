using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using System.Globalization;

namespace FDLocalization.Editor {
    [CustomPropertyDrawer(typeof(LanguageId)), CanEditMultipleObjects]
    public sealed class LanguageIdEditor : PropertyDrawer {

        static private NamedItemList<uint> s_LanguageIds; 

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (s_LanguageIds == null) {
                s_LanguageIds = new NamedItemList<uint>();
                foreach(var lang in CultureInfo.GetCultures(CultureTypes.NeutralCultures)) {
                    if (lang.ThreeLetterISOLanguageName.ToLowerInvariant() == "ivl") {
                        continue;
                    }
                    string name = lang.DisplayName;
                    name = string.Format("{0}/{1} ({2})", name[0], name, lang.ThreeLetterISOLanguageName);
                    s_LanguageIds.Add(CalculateValue(lang.ThreeLetterISOLanguageName), name);
                }
            }

            label = EditorGUI.BeginProperty(position, label, property);

            property.NextVisible(true);

            uint val = property.hasMultipleDifferentValues ? uint.MaxValue : (uint) property.longValue;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            uint newVal = (uint) ListGUI.Popup(position, label, val, s_LanguageIds);
            if (EditorGUI.EndChangeCheck()) {
                property.longValue = newVal;
            }
            EditorGUI.showMixedValue = false;

            EditorGUI.EndProperty();
        }

        static private uint CalculateValue(string threeLetterCode) {
            threeLetterCode = threeLetterCode ?? string.Empty;
            char a = threeLetterCode.Length > 0 ? char.ToLowerInvariant(threeLetterCode[0]) : (char) 0;
            char b = threeLetterCode.Length > 1 ? char.ToLowerInvariant(threeLetterCode[1]) : (char) 0;
            char c = threeLetterCode.Length > 2 ? char.ToLowerInvariant(threeLetterCode[2]) : (char) 0;
            char d = threeLetterCode.Length > 3 ? char.ToLowerInvariant(threeLetterCode[3]) : (char) 0;
            return (uint) (a) | ((uint) b << 8) | ((uint) c << 16) | ((uint) d << 24);
        }
    }
}