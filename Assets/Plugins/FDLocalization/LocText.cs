using System;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;
using TMPro;

namespace FDLocalization {
    /// <summary>
    /// Localized text component.
    /// </summary>
    [RequireComponent(typeof(TMP_Text)), DisallowMultipleComponent]
    public sealed class LocText : MonoBehaviour, ILocalizedComponent {

        public struct TextMetrics {
            public int VisibleCharCount;
            public int RichCharCount;
        }

        #region Inspector

        [SerializeField, HideInEditor] private TMP_Text m_Graphic = null;
        [SerializeField] internal LocId m_DefaultId = default(LocId);

        [Header("Modifications")]
        [SerializeField] private string m_Prefix = null;
        [SerializeField] private string m_Postfix = null;

        #endregion // Inspector

        [NonSerialized] private LocId m_LastId;
        [NonSerialized] private TextMetrics m_LastMetrics;
        [NonSerialized] private bool m_Initialized;
        [NonSerialized] private LanguageId m_LastKnownLanguage;

        /// <summary>
        /// Text renderer.
        /// </summary>
        public TMP_Text Graphic {
            get { return m_Graphic; }
        }

        /// <summary>
        /// Last metrics associated with this text field.
        /// </summary>
        public TextMetrics Metrics {
            get { return m_LastMetrics; }
        }

        /// <summary>
        /// Sets the current text as a localized string.
        /// </summary>
        public void SetText(LocId id, object context = null) {
            m_LastId = id;

            if (id.IsEmpty) {
                InternalSetText(string.Empty);
                m_LastMetrics = default;
                return;
            }

            TagString sharedTag = SharedTagString();
            if (Loc.TagWithContext(id, context, sharedTag)) {
                InternalSetText(sharedTag.RichText);
                m_LastMetrics.VisibleCharCount = sharedTag.VisibleText.Length;
                m_LastMetrics.RichCharCount = sharedTag.RichText.Length;
                sharedTag.Clear();
            } else {
                InternalSetText(ErrorString(id));
                m_LastMetrics.RichCharCount = m_Graphic.text.Length;
                m_LastMetrics.VisibleCharCount = m_Graphic.textInfo.characterCount;
                sharedTag.Clear();
            }
        }

        /// <summary>
        /// Sets the current text as a string.
        /// </summary>
        public void SetText(StringSlice text, object context = null) {
            m_LastId = default(LocId);

            if (text.IsEmpty) {
                InternalSetText(string.Empty);
                return;
            }

            TagString sharedTag = SharedTagString();
            Loc.TagWithContext(text, context, sharedTag);
            InternalSetText(sharedTag.RichText);
            m_LastMetrics.VisibleCharCount = sharedTag.VisibleText.Length;
            m_LastMetrics.RichCharCount = sharedTag.RichText.Length;
            sharedTag.Clear();
        }

        internal void InternalSetText(string text) {
            m_Graphic.SetText(PrePostString(text, m_Prefix, m_Postfix));
            m_Initialized = true;
            m_LastKnownLanguage = Loc.CurrentLanguage;
        }

        #region Events

        private void OnEnable() {
            Loc.RegisterComponent(this);

            if (Loc.IsReady) {
                if (!m_Initialized || m_LastKnownLanguage != Loc.CurrentLanguage) {
                    TryReload();
                }
            }
        }

        private void OnDisable() {
            Loc.DeregisterComponent(this);
        }

        void ILocalizedComponent.OnLocalizationReload(LanguageId languageId) {
            TryReload();
        }

        private void TryReload() {
            LocId id = !m_Initialized ? m_DefaultId : m_LastId;
            if (!id.IsEmpty) {
                SetText(id);
            } else {
                m_Initialized = true;
                m_LastKnownLanguage = Loc.CurrentLanguage;
            }
        }

        #endregion // Events

        #if UNITY_EDITOR

        private void Reset() {
            this.CacheComponent(ref m_Graphic);
        }

        private void OnValidate() {
            this.CacheComponent(ref m_Graphic);
        }

        #endif // UNITY_EDITOR

        #region Utils

        [ThreadStatic] static private TagString s_SharedTagString;

        static private TagString SharedTagString() {
            return s_SharedTagString ?? (s_SharedTagString = new TagString());
        }

        static private string ErrorString(LocId id) {
            return string.Format("<color=red>ERROR:</color> {0}", id.ToDebugString());
        }

        static internal unsafe string PrePostString(string text, string pre, string post) {
            pre = pre ?? string.Empty;
            post = post ?? string.Empty;
            text = text ?? string.Empty;

            int preLen = pre.Length;
            int postLen = post.Length;
            int textLen = text.Length;

            if (preLen + postLen <= 0) {
                return text;
            }

            int bufferSize = preLen + postLen + textLen;
            char* charBuffer = stackalloc char[bufferSize];

            char* head = charBuffer;

            if (preLen > 0) {
                fixed(char* preC = pre) {
                    Unsafe.CopyArray(preC, preLen, head);
                    head += preLen;
                }
            }

            if (textLen > 0) {
                fixed(char* textC = text) {
                    Unsafe.CopyArray(textC, textLen, head);
                    head += textLen;
                }
            }

            if (postLen > 0) {
                fixed(char* postC = post) {
                    Unsafe.CopyArray(postC, postLen, head);
                }
            }

            return new string(charBuffer, 0, bufferSize);
        }

        #endregion // Utils
    }
}