using UnityEngine;
using BeauUtil;
using System.Collections.Generic;
using BeauUtil.Debugger;
using System;
using FDLocalization;

namespace Journalism
{
    [CreateAssetMenu(menuName = "Journalism Content/Text Characters")]
    public sealed class TextChars : ScriptableObject
    {
        [Serializable]
        public class CharData : IKeyValuePair<StringHash32, CharData>
        {
            public SerializedHash32 Id;
            public LocId Name;

            public StringHash32 Key { get { return Id; } }
            public CharData Value { get { return this; } }
        }

        #region Inspector

        [SerializeField, Inline(InlineAttribute.DisplayType.HeaderLabel)] private CharData m_DefaultChar = null;

        [Header("Named Styles")]
        [SerializeField] private CharData[] m_Chars = null;

        #endregion // Inspector

        private Dictionary<StringHash32, CharData> m_CharMap;

        public CharData Default() {
            return m_DefaultChar;
        }

        public CharData Char(StringHash32 charId) {
            if (charId.IsEmpty) {
                return m_DefaultChar;
            }

            if (m_CharMap == null) {
                m_CharMap = m_Chars.CreateMap<StringHash32, CharData>();
            }

            if (!m_CharMap.TryGetValue(charId, out CharData data)) {
                Log.Error("[TextChars] No character with id '{0}' found!", charId);
                data = m_DefaultChar;
            }

            return data;
        }
    }
}