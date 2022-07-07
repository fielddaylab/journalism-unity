using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BeauUtil;
using UnityEngine;

namespace FDLocalization {
    /// <summary>
    /// Localization language id.
    /// </summary>
    [Serializable, StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct LanguageId : IEquatable<LanguageId>, IComparable<LanguageId> {

        #region Inspector

        [SerializeField, FieldOffset(0)] private uint m_Value;
        [NonSerialized, FieldOffset(0)] private byte m_0;
        [NonSerialized, FieldOffset(1)] private byte m_1;
        [NonSerialized, FieldOffset(2)] private byte m_2;
        [NonSerialized, FieldOffset(3)] private byte m_3;

        #endregion // Inspector

        public LanguageId(string threeLetterCode) {
            threeLetterCode = threeLetterCode ?? string.Empty;
            m_Value = 0;
            m_0 = threeLetterCode.Length > 0 ? (byte) char.ToLowerInvariant(threeLetterCode[0]) : (byte) 0;
            m_1 = threeLetterCode.Length > 1 ? (byte) char.ToLowerInvariant(threeLetterCode[1]) : (byte) 0;
            m_2 = threeLetterCode.Length > 2 ? (byte) char.ToLowerInvariant(threeLetterCode[2]) : (byte) 0;
            m_3 = threeLetterCode.Length > 3 ? (byte) char.ToLowerInvariant(threeLetterCode[3]) : (byte) 0;
        }

        public LanguageId(StringSlice threeLetterCode) {
            m_Value = 0;
            m_0 = threeLetterCode.Length > 0 ? (byte) char.ToLowerInvariant(threeLetterCode[0]) : (byte) 0;
            m_1 = threeLetterCode.Length > 1 ? (byte) char.ToLowerInvariant(threeLetterCode[1]) : (byte) 0;
            m_2 = threeLetterCode.Length > 2 ? (byte) char.ToLowerInvariant(threeLetterCode[2]) : (byte) 0;
            m_3 = threeLetterCode.Length > 3 ? (byte) char.ToLowerInvariant(threeLetterCode[3]) : (byte) 0;
        }

        public LanguageId(CultureInfo info)
            : this(info?.ThreeLetterISOLanguageName) {
        }

        public LanguageId(uint value) {
            m_0 = 0;
            m_1 = 0;
            m_2 = 0;
            m_3 = 0;
            m_Value = value;
        }

        public bool IsEmpty {
            [MethodImpl(256)] get { return m_Value == 0; }
        }

        public uint Value {
            [MethodImpl(256)] get { return m_Value; }
        }

        #region Interfaces

        public bool Equals(LanguageId other) {
            return m_Value == other.m_Value;
        }

        public int CompareTo(LanguageId other) {
            return m_Value.CompareTo(other.m_Value);
        }

        #endregion // Interfaces

        #region Overrides

        public override string ToString() {
            unsafe {
                char* buffer = stackalloc char[4];
                buffer[0] = m_0 == 0 ? ' ' : (char) m_0;
                buffer[1] = m_1 == 0 ? ' ' : (char) m_1;
                buffer[2] = m_2 == 0 ? ' ' : (char) m_2;
                buffer[3] = m_3 == 0 ? ' ' : (char) m_3;
                return new string(buffer, 0, 4);
            }
        }

        public override int GetHashCode() {
            return (int) m_Value;;
        }

        public override bool Equals(object obj) {
            if (obj is LanguageId) {
                return Equals((LanguageId) obj);
            } else {
                return false;
            }
        }

        #endregion // Overrides

        #region Operators

        static public bool operator ==(LanguageId left, LanguageId right) {
            return left.m_Value == right.m_Value;
        }

        static public bool operator !=(LanguageId left, LanguageId right) {
            return left.m_Value != right.m_Value;
        }

        #endregion // Operators
    
        static public readonly LanguageId English = new LanguageId("eng");
        static public readonly LanguageId Spanish = new LanguageId("spa");
        static public readonly LanguageId French = new LanguageId("fra");
        static public readonly LanguageId German = new LanguageId("ger");
        static public readonly LanguageId Italian = new LanguageId("ita");
        static public readonly LanguageId Dutch = new LanguageId("dut");
        static public readonly LanguageId Japanese = new LanguageId("jpn");

        /// <summary>
        /// Identifies a three-letter language code in a file path.
        /// File name should be of the format "fileName.code.strg" (ex. "mainText.eng.strg")
        /// </summary>
        static public LanguageId IdentifyLanguageFromPath(string filePath) {
            StringSlice pathWithoutExt;
            if (filePath.EndsWith(LocFile.FileExtensionWithDot)) {
                pathWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            } else {
                pathWithoutExt = Path.GetFileName(filePath);
            }

            if (pathWithoutExt.Length > 4 && pathWithoutExt[pathWithoutExt.Length - 4] == '.') {
                StringSlice langCode = pathWithoutExt.Substring(pathWithoutExt.Length - 3);
                return new LanguageId(langCode);
            } else {
                return default(LanguageId);
            }
        }
    }
}