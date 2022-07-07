using BeauUtil.Blocks;
using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif // UNITY_2020_2_OR_NEWER
#endif // UNITY_EDITOR

namespace FDLocalization {
    /// <summary>
    /// Localization file.
    /// </summary>
    public sealed class LocFile : CustomTextAsset {

        internal const string FileExtension = "strg";
        internal const string FileExtensionWithDot = ".strg";

        [SerializeField] private LanguageId m_Language;

        public LanguageId Language {
            get { return m_Language; }
        }
        
        #region Editor

        #if UNITY_EDITOR

        [ScriptedExtension(1, FileExtension)]
        private class Importer : ImporterBase<LocFile> {
            public override void OnImportAsset(AssetImportContext ctx) {
                base.OnImportAsset(ctx);

                LocFile file = (LocFile) ctx.mainObject;
                file.m_Language = LanguageId.IdentifyLanguageFromPath(ctx.assetPath);
                AssetDatabase.SaveAssets();
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}