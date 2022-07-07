using BeauUtil.Blocks;
using UnityEngine;
using System.IO;
using BeauUtil.Tags;
using BeauUtil;
using System.Collections.Generic;
using System;
using BeauUtil.Streaming;
using System.Collections;

namespace FDLocalization {
    /// <summary>
    /// Runtime localization database.
    /// </summary>
    public sealed class LocDB {
        private LocModule m_Module;

        private LocPackage m_Package;
        private LanguageId m_DefaultLanguage;
        
        private TagStringParser m_BaseParser;
        private readonly RingBuffer<TagStringParser> m_ParserPool = new RingBuffer<TagStringParser>();
        private readonly RingBuffer<TagString> m_TagStringPool = new RingBuffer<TagString>();
        private bool m_BaseParserInUse;

        public LocDB(LanguageId defaultLanguage) {
            m_DefaultLanguage = defaultLanguage;

            m_Module.Lookup = Get;
            m_Module.Tag = Tag;

            m_Package = new LocPackage("default");
        }

        #region Language

        public LanguageId DefaultLanguage {
            get { return m_DefaultLanguage; }
            set { m_DefaultLanguage = value; }
        }

        private bool IsDefaultLanguage() {
            return Loc.CurrentLanguage == m_DefaultLanguage;
        }

        private bool IsForCurrentLanguage(LocFile file) {
            return file.Language.IsEmpty || file.Language == Loc.CurrentLanguage;
        }

        #endregion // Language

        #region Parser

        /// <summary>
        /// Uses the given parser for tag parsing.
        /// </summary>
        public void UseParser(TagStringParser baseParser) {
            m_BaseParser = baseParser;
        }

        #endregion // Parser

        #region Loading

        /// <summary>
        /// Begins a loading block.
        /// This sets the current localization language
        /// and blocks lookups until EndLoading is called.
        /// </summary>
        public void BeginLoading(LanguageId langaugeId) {
            Loc.CurrentLanguage = langaugeId;
            Loc.IsReady = false;
        }

        /// <summary>
        /// Ends the loading block.
        /// </summary>
        public void EndLoading() {
            Loc.IsReady = true;
        }

        /// <summary>
        /// Loads a file.
        /// </summary>
        public void LoadFile(LocFile file) {
            if (!IsForCurrentLanguage(file)) {
                Debug.LogWarningFormat("[LocDB] Loading file '{0}' tagged for language '{1}' while current language is '{2}'", file.name, file.Language.ToString(), Loc.CurrentLanguage.ToString());
            }
            
            Loc.IsReady = false;
            BlockParser.Parse(ref m_Package, CharStreamParams.FromCustomTextAsset(file), BlockParsingRules.Default, LocPackage.Generator);
        }

        /// <summary>
        /// Loads a file asynchronously.
        /// Execute or iterate on the returned ienumerator to load.
        /// </summary>
        public IEnumerator LoadFileAsync(LocFile file) {
            if (!IsForCurrentLanguage(file)) {
                Debug.LogWarningFormat("[LocDB] Loading file '{0}' tagged for language '{1}' while current language is '{2}'", file.name, file.Language.ToString(), Loc.CurrentLanguage.ToString());
            }

            Loc.IsReady = false;
            return BlockParser.ParseAsync(ref m_Package, CharStreamParams.FromCustomTextAsset(file), BlockParsingRules.Default, LocPackage.Generator);
        }

        #endregion // Loading

        #region Unload

        /// <summary>
        /// Clears all localized strings and files.
        /// </summary>
        public void ClearFiles() {
            Loc.IsReady = false;
            m_Package.Clear();
        }

        /// <summary>
        /// Disposes of all resources owned by the localization database.
        /// </summary>
        public void DisposeResources() {
            m_ParserPool.Clear();
            m_BaseParser = null;
            m_BaseParserInUse = false;
            m_TagStringPool.Clear();
            m_Package.Clear();
            m_Package = null;
            m_DefaultLanguage = default;
            m_Module = default;
        }

        #endregion // Unload

        #region Module

        /// <summary>
        /// Localization code module.
        /// Register this with Loc.SetModule
        /// </summary>
        public LocModule Module {
            get { return m_Module; }
        }

        /// <summary>
        /// Sets this instance as the current localization module.
        /// </summary>
        public void SetAsCurrentModule() {
            Loc.SetModule(m_Module);
        }

        #endregion // Module

        #region Operations

        public string Get(LocId id, LocFlags flags, object context, StringSlice defaultResult = default(StringSlice)) {
            string result;
            bool wasDefault = false;
            if (!m_Package.TryGet(id, out result)) {
                if (IsDefaultLanguage() && !defaultResult.IsEmpty) {
                    result = defaultResult.ToString();
                    wasDefault = true;
                } else {
                    if ((flags & LocFlags.NoError) == 0) {
                        Debug.LogErrorFormat("[LocDB] Could not find string for '{0}'", id.ToDebugString());
                    }
                    return null;
                }
            }

            if (m_BaseParser != null && (flags & LocFlags.IgnoreTags) == 0 && (wasDefault || m_Package.HasPotentialTags(id))) {
                TagString tagString = GetTagString();
                TagStringParser parser = GetParser();
                try {
                    parser.Parse(ref tagString, result, context);
                    result = tagString.RichText;
                    if (tagString.EventCount > 0) {
                        Debug.LogWarningFormat("[LocDB] String for '{0}' contains {1} embedded events, which are discarded when getting a string directly", id.ToDebugString(), tagString.EventCount);
                    }
                    return result;
                } catch(Exception e) {
                    Debug.LogException(e);
                    return null;
                } finally {
                    ReturnParser(parser);
                    ReturnTagString(tagString);
                }
            } else {
                return result;
            }
        }

        public bool Tag(StringSlice text, LocFlags flags, object context, TagString output) {
            if (m_BaseParser == null) {
                Debug.LogWarningFormat("[LocDB] No base parser assigned; cannot parse to TagString");
                output.Clear();
                return false;
            }

            TagStringParser parser = GetParser();
            try {
                parser.Parse(ref output, text, context);
                return true;
            } catch(Exception e) {
                Debug.LogException(e);
                return false;
            }finally {
                ReturnParser(parser);
            }
        }

        #endregion // Operations

        #region Pools

        private TagStringParser GetParser() {
            if (!m_BaseParserInUse) {
                m_BaseParserInUse = true;
                return m_BaseParser;
            }

            TagStringParser parser;
            if (!m_ParserPool.TryPopBack(out parser)) {
                parser = new TagStringParser();
                parser.Delimiters = m_BaseParser.Delimiters;
                parser.EventProcessor = m_BaseParser.EventProcessor;
                parser.ReplaceProcessor = m_BaseParser.ReplaceProcessor;
            }

            return parser;
        }

        private void ReturnParser(TagStringParser parser) {
            if (m_BaseParserInUse && m_BaseParser == parser) {
                m_BaseParserInUse = false;
                return;
            }

            m_ParserPool.PushBack(parser);
        }

        private TagString GetTagString() {
            TagString tagString;
            if (!m_TagStringPool.TryPopBack(out tagString)) {
                tagString = new TagString();
            }
            return tagString;
        }

        private void ReturnTagString(TagString tagString) {
            tagString.Clear();
            m_TagStringPool.PushBack(tagString);
        }

        #endregion // Pools
    }
}