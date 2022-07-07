#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
#define PRESERVE_DEBUG_SYMBOLS
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT

#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT
#define LOC_ERROR_CHECKING
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD || DEVELOPMENT

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BeauUtil;
using BeauUtil.Tags;
using UnityEngine;

[assembly: InternalsVisibleTo("FDLocalization.Editor")]

namespace FDLocalization
{
    static public class Loc {

        #region Callbacks

        static private LocModule s_CurrentModule;

        /// <summary>
        /// Sets the current localization module.
        /// </summary>
        static public void SetModule(LocModule module) {
            s_CurrentModule = module;
        }

        #endregion // Callbacks

        #region Current

        static private LanguageId s_CurrentLanguage;
        static private bool s_Ready;

        /// <summary>
        /// Gets/sets the current language.
        /// </summary>
        static public LanguageId CurrentLanguage {
            [MethodImpl(256)] get { return s_CurrentLanguage; }
            set {
                if (value != s_CurrentLanguage) {
                    s_CurrentLanguage = value;
                    if (s_Ready) {
                        ReloadComponents();
                    }
                }
            }
        }

        /// <summary>
        /// Flag indicating if localization data is loaded.
        /// </summary>
        static public bool IsReady {
            [MethodImpl(256)] get { return s_Ready; }
            set {
                if (s_Ready != value) {
                    s_Ready = value;
                    if (s_Ready) {
                        ReloadComponents();
                    }
                }
            }
        }

        #endregion // Current

        #region Components

        static private readonly List<ILocalizedComponent> s_LocalizedComponents = new List<ILocalizedComponent>(32);

        /// <summary>
        /// Registers a localized component.
        /// </summary>
        static public void RegisterComponent(ILocalizedComponent component) {
            s_LocalizedComponents.Add(component);
        }

        /// <summary>
        /// Deregisters a localized component.
        /// </summary>
        static public void DeregisterComponent(ILocalizedComponent component) {
            s_LocalizedComponents.FastRemove(component);
        }

        /// <summary>
        /// Reloads all registered components.
        /// </summary>
        static public void ReloadComponents() {
            foreach(var obj in s_LocalizedComponents) {
                obj.OnLocalizationReload(s_CurrentLanguage);
            }
        }

        #endregion // Components

        #region Get

        /// <summary>
        /// Retrieves the string associated with the given id.
        /// Will parse any tags.
        /// </summary>
        [MethodImpl(256)]
        static public string Get(LocId id) {
            if (!IsReady) {
                return null;
            }
            return s_CurrentModule.Lookup?.Invoke(id, 0, null, null);
        }

        /// <summary>
        /// Retrieves the string associated with the given id.
        /// Will parse any tags.
        /// </summary>
        [MethodImpl(256)]
        static public string Get(LocId id, StringSlice defaultString) {
            if (!IsReady) {
                return null;
            }
            return s_CurrentModule.Lookup?.Invoke(id, 0, null, defaultString) ?? defaultString.ToString();
        }

        /// <summary>
        /// Retrieves the string associated with the given id.
        /// Will parse any tags.
        /// </summary>
        [MethodImpl(256)]
        static public string GetWithContext(LocId id, object context) {
            if (!IsReady) {
                return null;
            }
            return s_CurrentModule.Lookup?.Invoke(id, 0, context, null);
        }

        /// <summary>
        /// Retrieves the string associated with the given id.
        /// Will parse any tags.
        /// </summary>
        [MethodImpl(256)]
        static public string GetWithContext(LocId id, object context, StringSlice defaultString) {
            if (!IsReady) {
                return null;
            }
            return s_CurrentModule.Lookup?.Invoke(id, 0, context, defaultString) ?? defaultString.ToString();
        }

        /// <summary>
        /// Attempts to retrieve the string associated with the given id.
        /// Will parse any tags.
        /// </summary>
        [MethodImpl(256)]
        static public bool TryGet(LocId id, out string result) {
            if (!IsReady) {
                result = null;
                return false;
            }
            result = s_CurrentModule.Lookup?.Invoke(id, LocFlags.NoError, null, null);
            return result != null;
        }

        /// <summary>
        /// Attempts to retrieve the string associated with the given id.
        /// Will parse any tags.
        /// </summary>
        [MethodImpl(256)]
        static public bool TryGet(LocId id, StringSlice defaultString, out string result) {
            if (!IsReady) {
                result = null;
                return false;
            }
            result = s_CurrentModule.Lookup?.Invoke(id, LocFlags.NoError, null, defaultString) ?? defaultString.ToString();
            return result != null;
        }

        /// <summary>
        /// Attempts to retrieve the string associated with the given id.
        /// Will parse any tags.
        /// </summary>
        [MethodImpl(256)]
        static public bool TryGetWithContext(LocId id, object context, out string result) {
            if (!IsReady) {
                result = null;
                return false;
            }
            result = s_CurrentModule.Lookup?.Invoke(id, LocFlags.NoError, context, null);
            return result != null;
        }

        /// <summary>
        /// Attempts to retrieve the string associated with the given id.
        /// Will parse any tags.
        /// </summary>
        [MethodImpl(256)]
        static public bool TryGetWithContext(LocId id, object context, StringSlice defaultString, out string result) {
            if (!IsReady) {
                result = null;
                return false;
            }
            result = s_CurrentModule.Lookup?.Invoke(id, LocFlags.NoError, context, defaultString) ?? defaultString.ToString();
            return result != null;
        }

        #endregion // Get

        #region Direct

        /// <summary>
        /// Retrieves the string directly, with no tag processing.
        /// </summary>
        [MethodImpl(256)]
        static public string Direct(LocId id) {
            if (!IsReady) {
                return null;
            }
            return s_CurrentModule.Lookup?.Invoke(id, LocFlags.IgnoreTags, null, null);
        }

        /// <summary>
        /// Retrieves the string directly, with no tag processing.
        /// </summary>
        [MethodImpl(256)]
        static public string Direct(LocId id, StringSlice defaultString) {
            if (!IsReady) {
                return null;
            }
            return s_CurrentModule.Lookup?.Invoke(id, LocFlags.IgnoreTags, null, defaultString) ?? defaultString.ToString();
        }

        /// <summary>
        /// Attempts to retrieve the string directly, with no tag processing.
        /// </summary>
        static public bool TryDirect(LocId id, out string result) {
            if (!IsReady) {
                result = null;
                return false;
            }
            result = s_CurrentModule.Lookup?.Invoke(id, LocFlags.IgnoreTags | LocFlags.NoError, null, null);
            return result != null;
        }

        /// <summary>
        /// Attempts to retrieve the string directly, with no tag processing.
        /// </summary>
        static public bool TryDirect(LocId id, StringSlice defaultString, out string result) {
            if (!IsReady) {
                result = null;
                return false;
            }
            result = s_CurrentModule.Lookup?.Invoke(id, LocFlags.IgnoreTags | LocFlags.NoError, null, defaultString);
            return result != null;
        }

        #endregion // Direct

        #region Tag

        /// <summary>
        /// Retrieves and parses the string associated with the given id into a TagString.
        /// </summary>
        [MethodImpl(256)]
        static public bool Tag(LocId id, TagString output) {
            string text = Direct(id);
            if (text != null) {
                return Tag(text, output);
            } else {
                output.Clear();
                return false;
            }
        }

        /// <summary>
        /// Retrieves and parses the string associated with the given id into a TagString.
        /// </summary>
        [MethodImpl(256)]
        static public bool Tag(LocId id, TagString output, StringSlice defaultString) {
            string text = Direct(id, defaultString);
            if (text != null) {
                return Tag(text, output);
            } else {
                output.Clear();
                return false;
            }
        }

        /// <summary>
        /// Parses the given string into a TagString.
        /// </summary>
        [MethodImpl(256)]
        static public bool Tag(StringSlice text, TagString output) {
            return Tag(text.ToString(), output);
        }

        /// <summary>
        /// Parses the given string into a TagString.
        /// </summary>
        [MethodImpl(256)]
        static public bool Tag(string text, TagString output) {
            if (s_CurrentModule.Tag != null) {
                return s_CurrentModule.Tag(text, 0, null, output);
            } else {
                output.Clear();
                output.VisibleText = output.RichText = text;
                output.AddText(0u, (uint) output.VisibleText.Length);
                return false;
            }
        }

        /// <summary>
        /// Retrieves and parses the string associated with the given id into a TagString.
        /// </summary>
        [MethodImpl(256)]
        static public bool TagWithContext(LocId id, object context, TagString output) {
            string text = Direct(id);
            if (text != null) {
                return TagWithContext(text, context, output);
            } else {
                output.Clear();
                return false;
            }
        }

        /// <summary>
        /// Retrieves and parses the string associated with the given id into a TagString.
        /// </summary>
        [MethodImpl(256)]
        static public bool TagWithContext(LocId id, object context, TagString output, StringSlice defaultString) {
            string text = Direct(id, defaultString);
            if (text != null) {
                return TagWithContext(text, context, output);
            } else {
                output.Clear();
                return false;
            }
        }

        /// <summary>
        /// Parses the given string into a TagString.
        /// </summary>
        [MethodImpl(256)]
        static public bool TagWithContext(StringSlice text, object context, TagString output) {
            return TagWithContext(text.ToString(), context, output);
        }

        /// <summary>
        /// Parses the given string into a TagString.
        /// </summary>
        [MethodImpl(256)]
        static public bool TagWithContext(string text, object context, TagString output) {
            if (s_CurrentModule.Tag != null) {
                return s_CurrentModule.Tag(text, 0, context, output);
            } else {
                output.Clear();
                output.VisibleText = output.RichText = text;
                output.AddText(0u, (uint) output.VisibleText.Length);
                return false;
            }
        }

        #endregion // Tag

        #region Format

        /// <summary>
        /// Localizes the given string and substitutes c# string.format tags
        /// with the provided arguments.
        /// </summary>
        static public string Format(LocId id, object arg0) {
            string format = Get(id);
            ProcessForFormat(null, ref arg0);
            #if LOC_ERROR_CHECKING
            try {
                return string.Format(format, arg0);
            } catch(FormatException e) {
                throw new FormatException("Could not format string for " + id.ToDebugString() + ": " + format, e);
            }
            #else
            return string.Format(format, arg0);
            #endif // LOC_ERROR_CHECKING
        }

        /// <summary>
        /// Localizes the given string and substitutes c# string.format tags
        /// with the provided arguments.
        /// </summary>
        static public string Format(LocId id, object arg0, object arg1) {
            string format = Get(id);
            ProcessForFormat(null, ref arg0);
            ProcessForFormat(null, ref arg1);
            #if LOC_ERROR_CHECKING
            try {
                return string.Format(format, arg0, arg1);
            } catch(FormatException e) {
                throw new FormatException("Could not format string for " + id.ToDebugString() + ": " + format, e);
            }
            #else
            return string.Format(format, arg0, arg1);
            #endif // LOC_ERROR_CHECKING
        }

        /// <summary>
        /// Localizes the given string and substitutes c# string.format tags
        /// with the provided arguments.
        /// </summary>
        static public string Format(LocId id, object arg0, object arg1, object arg2) {
            string format = Get(id);
            ProcessForFormat(null, ref arg0);
            ProcessForFormat(null, ref arg1);
            ProcessForFormat(null, ref arg2);
            #if LOC_ERROR_CHECKING
            try {
                return string.Format(format, arg0, arg1, arg2);
            } catch(FormatException e) {
                throw new FormatException("Could not format string for " + id.ToDebugString() + ": " + format, e);
            }
            #else
            return string.Format(format, arg0, arg1, arg2);
            #endif // LOC_ERROR_CHECKING
        }

        /// <summary>
        /// Localizes the given string and substitutes c# string.format tags
        /// with the provided arguments.
        /// </summary>
        static public string Format(LocId id, params object[] args) {
            string format = Get(id);
            if (args != null) {
                for(int i = 0; i < args.Length; i++) {
                    ProcessForFormat(null, ref args[i]);
                }
            }
            #if LOC_ERROR_CHECKING
            try {
                return string.Format(format, args);
            } catch(FormatException e) {
                throw new FormatException("Could not format string for " + id.ToDebugString() + ": " + format, e);
            }
            #else
            return string.Format(format, args);
            #endif // LOC_ERROR_CHECKING
        }

        static private void ProcessForFormat(object context, ref object arg) {
            if (arg is LocId) {
                arg = s_CurrentModule.Lookup?.Invoke((LocId) arg, LocFlags.IgnoreTags, context, null);
            } else if (arg is StringHash32) {
                arg = s_CurrentModule.Lookup?.Invoke(new LocId((StringHash32) arg), LocFlags.IgnoreTags, context, null);
            }
        }

        #endregion // Format

        #region Extensions

        [MethodImpl(256)]
        static public StringBuilder AppendLoc(this StringBuilder builder, LocId id) {
            return builder.Append(Get(id));
        }

        [MethodImpl(256)]
        static public StringBuilder AppendLoc(this StringBuilder builder, LocId id, LocTextTransform textTransform) {
            return builder.Append(textTransform(Get(id)));
        }

        #endregion // Extensions
    }

    /// <summary>
    /// Localization callbacks.
    /// </summary>
    public struct LocModule {
        public LocLookupDelegate Lookup;
        public LocTagDelegate Tag;
    }

    /// <summary>
    /// Delegate for looking up a localized string.
    /// </summary>
    public delegate string LocLookupDelegate(LocId id, LocFlags flags, object context, StringSlice defaultResult = default(StringSlice));
    
    /// <summary>
    /// Delegate for parsing a string to a TagString.
    /// </summary>
    public delegate bool LocTagDelegate(StringSlice text, LocFlags flags, object context, TagString output);
    
    /// <summary>
    /// Delegate for modifying a string.
    /// </summary>
    public delegate string LocTextTransform(string text);

    /// <summary>
    /// Localization flags.
    /// </summary>
    public enum LocFlags : uint {
        IgnoreTags = 0x01,
        NoError = 0x02
    }

    /// <summary>
    /// Interface for all localized components.
    /// </summary>
    public interface ILocalizedComponent {
        void OnLocalizationReload(LanguageId languageId);
    }
}