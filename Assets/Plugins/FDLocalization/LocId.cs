#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
#define PRESERVE_DEBUG_SYMBOLS
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT

#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
#define LOC_ERROR_CHECKING
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using BeauUtil;
using UnityEngine;

namespace FDLocalization {
    /// <summary>
    /// Localization id.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToDebugString()}")]
    public struct LocId : IDebugString, IEquatable<LocId>, IEquatable<StringHash32>, IComparable<LocId>
#if UNITY_EDITOR
        , ISerializationCallbackReceiver
#endif // UNITY_EDITOR
    {

        #region Inspector

        [SerializeField] private string m_Source;
        [SerializeField] private uint m_HashValue;

        #endregion // Inspector

        public LocId(string source) {
            m_Source = source;
            m_HashValue = new StringHash32(source).HashValue;
        }

        public LocId(StringSlice source) {
            m_Source = source.ToString();
            m_HashValue = new StringHash32(source).HashValue;
        }

        public LocId(StringHash32 hash) {
            m_Source = null;
            m_HashValue = hash.HashValue;
        }

        public bool IsEmpty {
            [MethodImpl(256)] get { return m_HashValue == 0; }
        }

        [MethodImpl(256)]
        public string Source() {
            return m_Source;
        }

        [MethodImpl(256)]
        public StringHash32 Hash() {
            return new StringHash32(m_HashValue);
        }

        #region Interfaces

        public string ToDebugString() {
            return Hash().ToDebugString();
        }

        public bool Equals(LocId other) {
            return other.m_HashValue == m_HashValue;
        }

        public bool Equals(StringHash32 other) {
            return other.HashValue == m_HashValue;
        }

        public int CompareTo(LocId other) {
            return  m_HashValue < other.m_HashValue ? -1 : (m_HashValue > other.m_HashValue ? 1 : 0);
        }

        #if UNITY_EDITOR

        public void OnBeforeSerialize() {
        }

        public void OnAfterDeserialize() {
            if (!string.IsNullOrEmpty(m_Source)) {
                new StringHash32(m_Source);
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Interfaces

        #region Overrides

        public override string ToString() {
            return Hash().ToString();
        }

        public override int GetHashCode() {
            return (int) m_HashValue;
        }

        public override bool Equals(object obj) {
            if (obj is LocId) {
                return Equals((LocId) obj);
            } else if (obj is StringHash32) {
                return Equals((StringHash32) obj);
            } else {
                return false;
            }
        }

        #endregion // Overrides

        #region Operators

        static public bool operator ==(LocId left, LocId right) {
            return left.m_HashValue == right.m_HashValue;
        }

        static public bool operator !=(LocId left, LocId right) {
            return left.m_HashValue != right.m_HashValue;
        }

        static public explicit operator LocId(string source) {
            return new LocId(source);
        }

        static public explicit operator LocId(StringSlice source) {
            return new LocId(source);
        }

        static public explicit operator LocId(StringHash32 hash) {
            return new LocId(hash);
        }

        #endregion // Operators
    }
}