using UnityEngine;
using BeauUtil;
using System.Collections.Generic;
using BeauUtil.Debugger;
using System;

namespace Journalism {
    [CreateAssetMenu(menuName = "Journalism Content/Text Styles")]
    public sealed class TextStyles : ScriptableObject {
        [Serializable]
        public class StyleData : IKeyValuePair<StringHash32, StyleData> {
            public SerializedHash32 Id;
            public Color32 Background;
            public Color32 Outline;
            public TextLine.TailMode Tail;
            public float RoundingScale = 1.5f;

            public StringHash32 Key { get { return Id; } }
            public StyleData Value { get { return this; } }
        }

        #region Inspector

        [SerializeField, Inline(InlineAttribute.DisplayType.HeaderLabel)] private StyleData m_DefaultStyle = null;

        [Header("Named Styles")]
        [SerializeField] private StyleData[] m_Styles = null;

        #endregion // Inspector

        private Dictionary<StringHash32, StyleData> m_StyleMap;

        public StyleData Style(StringHash32 styleId) {
            if (styleId.IsEmpty) {
                return m_DefaultStyle;
            }

            if (m_StyleMap == null) {
                m_StyleMap = m_Styles.CreateMap<StringHash32, StyleData>();
            }

            if (!m_StyleMap.TryGetValue(styleId, out StyleData data)) {
                Log.Error("[TextStyles] No style with id '{0}' found!", styleId);
                data = m_DefaultStyle;
            }

            return data;
        }
    }
}