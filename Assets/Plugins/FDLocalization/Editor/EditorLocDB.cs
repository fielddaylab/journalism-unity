using UnityEngine;
using UnityEditor;
using BeauUtil.Editor;
using System.Globalization;
using System;
using BeauUtil.Blocks;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Scripting;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Streaming;
using System.IO;
using UnityEditorInternal;
using System.CodeDom.Compiler;

namespace FDLocalization.Editor {
    public sealed class EditorLocDB : ScriptableObject {
        #region Singleton
        
        static private EditorLocDB s_Instance;

        public const string AssetPath = "Assets/Editor/EditorLocDB.asset";

        static private EditorLocDB GetInstance() {
            if (!s_Instance) {
                s_Instance = AssetDatabase.LoadAssetAtPath<EditorLocDB>(AssetPath);
                if (!s_Instance) {
                    s_Instance = CreateInstance<EditorLocDB>();
                    AssetDatabase.CreateAsset(s_Instance, AssetPath);
                    AssetDatabase.SaveAssets();
                }
            }
            return s_Instance;
        }

        static private EditorLocDB GetInitialized() {
            var db = GetInstance();
            db.FinishInit();
            return db;
        }

        #endregion // Singleton

        #region Types

        [Serializable]
        private struct BasePathHeader {
            public string Path;
            public int RecordStart;

            public BasePathHeader(string path, int start) {
                Path = path;
                RecordStart = start;
            }
        }

        [Serializable]
        private class TextRecord : IDataBlock {
            public string Id;
            [Multiline, BlockContent, Preserve] public string Content;
            public bool Exposed;

            [BlockMeta("exposed"), Preserve]
            private void MarkExposed() {
                Exposed = true;
            }

            [NonSerialized] public FileRecord Parent;

            public TextRecord(string id, FileRecord parent) {
                Id = id;
                Parent = parent;
            }
        }

        [Serializable]
        private class FileRecord : IDataBlockPackage<TextRecord> {
            public string Name;

            public LocFile Asset;
            [HideInInspector] public string AssetPath;

            public List<TextRecord> Records = new List<TextRecord>();
            public List<BasePathHeader> Headers = new List<BasePathHeader>();

            [HideInInspector] public string BasePath = string.Empty;
            [HideInInspector] public bool Dirty = false;

            public FileRecord(string name) {
                Name = name;
            }

            public int Count {
                get { return Records.Count; }
            }

            public IEnumerator<TextRecord> GetEnumerator() {
                return Records.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }

            [BlockMeta("basePath"), Preserve]
            private void SetBasePath(string path) {
                BasePath = path;
                Headers.Add(new BasePathHeader(path, Records.Count));
            }
        }

        private class Parser : AbstractBlockGenerator<TextRecord, FileRecord> {
            public static readonly Parser Instance = new Parser();

            public override FileRecord CreatePackage(string inFileName) {
                return new FileRecord(inFileName);
            }

            public override bool TryCreateBlock(IBlockParserUtil inUtil, FileRecord inPackage, TagData inId, out TextRecord outBlock) {
                StringSlice id = inId.Id;
                StringSlice root = inPackage.BasePath;
                string name;
                if (id.StartsWith('.')) {
                    id = id.Substring(1);
                }

                if (!root.IsEmpty) {
                    if (root.EndsWith('.')) {
                        root = root.Substring(0, root.Length - 1);
                    }

                    inUtil.TempBuilder.Length = 0;
                    inUtil.TempBuilder.AppendSlice(root)
                        .Append('.').AppendSlice(id);

                    name = inUtil.TempBuilder.Flush();
                } else {
                    name = id.ToString();
                }

                outBlock = new TextRecord(name, inPackage);
                inPackage.Records.Add(outBlock);
                return true;
            }
        }

        #endregion // Types

        #region Inspector

        [SerializeField] private LanguageId m_DefaultLanguage = default;

        [Header("All Files")]
        [SerializeField] private List<FileRecord> m_FileRecords = new List<FileRecord>();
        private Dictionary<uint, TextRecord> m_TextMap = new Dictionary<uint, TextRecord>(128);
        [NonSerialized] private bool m_Initialized;
        [SerializeField, HideInInspector] private string m_LastExportClassName;
        [SerializeField, HideInInspector] private string m_LastExportPath;

        static private bool s_ImportLocked;

        #endregion // Inspector

        #region Callbacks

        [InitializeOnLoadMethod]
        static private void EditorInit() {
            if (InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs) {
                return;
            }

            var instance = GetInstance();
            if (instance.m_FileRecords.Count == 0) {
                instance.ReloadFiles();
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            if (!EditorApplication.isPlayingOrWillChangePlaymode) {
                LoadLocModule(instance.m_DefaultLanguage);
            }
        }

        static private void OnPlayModeStateChanged(PlayModeStateChange stateChange) {
            switch(stateChange) {
                case PlayModeStateChange.ExitingEditMode: {
                    if (EditorUtility.IsDirty(GetInstance())) {
                        WriteAnyChanges();
                    }
                    break;
                }
                case PlayModeStateChange.EnteredEditMode: {
                    LoadLocModule(GetInstance().m_DefaultLanguage);
                    break;
                }
            }
        }

        static private void LoadLocModule(LanguageId language) {
            LocModule module;
            module.Lookup = LocLookup;
            module.Tag = null;
            Loc.SetModule(module);
            Loc.CurrentLanguage = language;
            Loc.IsReady = true;
        }

        private class AssetSaveHook : UnityEditor.AssetModificationProcessor {
            static private string[] OnWillSaveAssets(string[] paths) {
                if (!s_ImportLocked) {
                    foreach (var path in paths) {
                        if (path == AssetPath) {
                            WriteAnyChanges();
                            break;
                        }
                    }
                }
                return paths;
            }
        }

        private class AssetImportHook : AssetPostprocessor {
            static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
                if (s_ImportLocked || Application.isPlaying || InternalEditorUtility.inBatchMode || !InternalEditorUtility.isHumanControllingUs)
                    return;

                LanguageId defaultLanguage = GetInstance().m_DefaultLanguage;

                if (AnyAreLocPackage(importedAssets, defaultLanguage) || AnyAreLocPackage(deletedAssets, defaultLanguage) || AnyAreLocPackage(movedAssets, defaultLanguage) || AnyAreLocPackage(movedFromAssetPaths, defaultLanguage)) {
                    EditorApplication.delayCall += () => GetInstance().ReloadFiles();
                }
            }

            static private bool AnyAreLocPackage(string[] assetNames, LanguageId defaultLanguage) {
                if (assetNames == null || assetNames.Length == 0)
                    return false;

                foreach (var filePath in assetNames) {
                    if (filePath.EndsWith("EditorLocDB.cs"))
                        return true;

                    if (filePath.EndsWith(".strg")) {
                        LanguageId lang = LanguageId.IdentifyLanguageFromPath(filePath);
                        return lang.IsEmpty || lang == defaultLanguage;
                    }
                }

                return false;
            }
        }

        #endregion // Callbacks

        #region Public

        /// <summary>
        /// Attempts to lookup an existing string.
        /// </summary>
        static public bool TryLookup(string key, out string text) {
            var instance = GetInitialized();
            TextRecord record;
            if (instance.m_TextMap.TryGetValue(KeyHash(key), out record)) {
                text = record.Content;
                return true;
            } else {
                text = null;
                return false;
            }
        }

        static private string LocLookup(LocId id, LocFlags flags, object context, StringSlice defaultResult = default(StringSlice)) {
            string result;
            if (!TryLookup(id.ToDebugString(), out result)) {
                result = defaultResult.ToString();
            }
            return result;
        }

        /// <summary>
        /// Attempts to overwrite or insert a string.
        /// </summary>
        static public void TrySet(string key, string text) {
            GetInitialized().WriteInsert(key, text);
        }

        /// <summary>
        /// Writes any pending changes to disk.
        /// </summary>
        static public void WriteAnyChanges() {
            var instance = GetInitialized();
            bool hadChanges = false;
            foreach(var file in instance.m_FileRecords) {
                hadChanges |= WriteChangesToFile(file, false);
            }

            if (!hadChanges) {
                Debug.Log("[EditorLocDB] No changes to export");
            }
        }

        /// <summary>
        /// Searches for any keys that contain the given search string.
        /// </summary>
        static public int Search(string search, ICollection<string> results) {
            var instance = GetInitialized();
            
            int count = 0;
            foreach (var record in instance.m_TextMap.Values) {
                if (record.Id.Contains(search)) {
                    results.Add(record.Id);
                    count++;
                }
            }

            return count;
        }

        #endregion // Public

        #region Operations

        private void ReloadFiles() {
            bool hadFiles = m_FileRecords.Count > 0;

            m_FileRecords.Clear();
            m_TextMap.Clear();
            m_Initialized = false;

            List<LocFile> defaultFiles = new List<LocFile>(AssetDBUtils.FindAssets<LocFile>());
            try {
                LocFile file;
                for(int i = defaultFiles.Count - 1; i >= 0; i--) {
                    file = defaultFiles[i];
                    if (!file.Language.IsEmpty && file.Language != m_DefaultLanguage) {
                        defaultFiles.FastRemoveAt(i);
                    }
                }

                int count = defaultFiles.Count;
                if (count == 0) {
                    if (hadFiles) {
                        Debug.LogFormat("[EditorLocDB] Could not find files");
                    }
                } else {
                    Debug.LogFormat("[EditorLocDB] Rebuilding database...");
                    for(int i = 0; i < count; i++) {
                        file = defaultFiles[i];
                        string filePath = AssetDatabase.GetAssetPath(file);

                        Debug.LogFormat("[EditorLocDB] ...importing {0}", filePath);
                        EditorUtility.DisplayProgressBar("Updated Editor Localization database", string.Format("Importing {0}/{1}: {2}", i + 1, count, filePath), (float) (i + 1) / count);

                        FileRecord record = BlockParser.Parse(CharStreamParams.FromStream(File.OpenRead(filePath), null, true, file.name), BlockParsingRules.Default, Parser.Instance);
                        record.AssetPath = filePath;
                        record.Asset = file;

                        m_FileRecords.Add(record);
                    }
                }
            } finally {
                FinishInit();
                EditorUtility.ClearProgressBar();
            }

            EditorUtility.SetDirty(this);
        }

        private void FinishInit() {
            if (m_Initialized) {
                return;
            }

            m_TextMap.Clear();

            foreach(var file in m_FileRecords) {
                foreach(var text in file.Records) {
                    uint key = KeyHash(text.Id);
                    if (m_TextMap.ContainsKey(key)) {
                        Debug.LogErrorFormat("[EditorLocDB] Duplicate localization key '{0}'", text.Id);
                    } else {
                        m_TextMap.Add(key, text);
                    }
                }
            }

            m_Initialized = true;
            GC.Collect();
        }

        private void WriteInsert(string key, string text) {
            TextRecord existingRecord;
            uint keyHash = KeyHash(key);
            if (m_TextMap.TryGetValue(keyHash, out existingRecord)) {
                if (existingRecord.Content != text) {
                    existingRecord.Content = text;
                    existingRecord.Parent.Dirty = true;
                    EditorUtility.SetDirty(this);
                }
            } else {
                FileRecord targetFile = null;
                TextRecord newRecord = null;
                bool bInserted = false;
                foreach (var file in m_FileRecords) {
                    if (file.Headers.Count == 0) {
                        targetFile = file;
                        continue;
                    }
                    
                    BasePathHeader basePath;
                    int bestMatchIdx = -1;
                    int bestMatchLength = 0;
                    for (int i = 0, totalPathCount = file.Headers.Count; i < totalPathCount; i++) {
                        basePath = file.Headers[i];
                        if (basePath.Path.Length > bestMatchLength && key.StartsWith(basePath.Path, StringComparison.InvariantCulture)) {
                            bestMatchLength = basePath.Path.Length;
                            bestMatchIdx = i;
                        }
                    }

                    if (bestMatchIdx >= 0) {
                        newRecord = InsertTextRecord(key, text, file, bestMatchIdx);
                        EditorUtility.SetDirty(this);
                        bInserted = true;
                        break;
                    }
                }

                if (!bInserted && targetFile != null) {
                    newRecord = InsertTextRecord(key, text, targetFile, 0);
                    EditorUtility.SetDirty(this);
                    bInserted = true;
                }

                if (!bInserted) {
                    Debug.LogErrorFormat("[EditorLocDB] No valid file located to insert text with id '{0}'", key);
                } else {
                    Debug.LogFormat("[EditorLocDB] Successfully added text with key '{0}' to file '{1}'", key, newRecord.Parent.Name);
                    m_TextMap[keyHash] = newRecord;
                }
            }
        }

        private void TryWriteConsts() {
            List<string> exposed = new List<string>(m_TextMap.Count);
            foreach(var text in m_TextMap.Values) {
                if (text.Exposed) {
                    exposed.Add(text.Id);
                }
            }

            if (exposed.Count == 0) {
                Debug.LogWarningFormat("[EditorLocDB] No exposed strings to export");
                return;
            }

            var wizard = ScriptableWizard.DisplayWizard<ExportWizard>("Export to Code File", "Export");
            wizard.Ids = exposed.ToArray();
            wizard.ClassName = string.IsNullOrEmpty(m_LastExportClassName) ? "TextConsts" : m_LastExportClassName;
            wizard.LastPath = m_LastExportPath;
        }

        static private TextRecord InsertTextRecord(string key, string text, FileRecord file, int secondId) {
            TextRecord textRecord = new TextRecord(key, file);
            textRecord.Content = text;
            textRecord.Parent = file;

            if (secondId >= file.Headers.Count - 1) {
                file.Records.Add(textRecord);
            } else {
                BasePathHeader nextRecord = file.Headers[secondId + 1];
                int insertIdx = nextRecord.RecordStart;

                file.Records.Insert(insertIdx, textRecord);

                for (int i = secondId + 1; i < file.Headers.Count; i++) {
                    BasePathHeader revisedHeader = file.Headers[i];
                    revisedHeader.RecordStart++;
                    file.Headers[i] = revisedHeader;
                }
            }

            file.Dirty = true;
            return textRecord;
        }

        static private bool WriteChangesToFile(FileRecord file, bool force) {
            if (!force && !file.Dirty) {
                return false;
            }

            using(var writer = new StreamWriter(File.Open(file.AssetPath, FileMode.Create))) {
                int recordIdx = 0, pathIdx = 0;
                int totalRecordCount = file.Records.Count;
                int totalPathCount = file.Headers.Count;
                BasePathHeader nextBasePath = totalPathCount > 0 ? file.Headers[0] : default;
                TextRecord currentRecord;
                string currentBasePath = string.Empty;

                while (recordIdx < totalRecordCount) {
                    while (pathIdx < totalPathCount && recordIdx >= nextBasePath.RecordStart) {
                        writer.Write("# basePath ");
                        writer.Write(nextBasePath.Path);
                        writer.Write("\n\n");
                        currentBasePath = nextBasePath.Path;
                        pathIdx++;
                        nextBasePath = pathIdx < totalPathCount ? file.Headers[pathIdx] : default;
                    }

                    currentRecord = file.Records[recordIdx];
                    writer.Write(":: ");

                    string recordId = currentRecord.Id;
                    if (!string.IsNullOrEmpty(currentBasePath)) {
                        recordId = recordId.Substring(currentBasePath.Length + 1);
                        if (recordId.Length == 0) {
                            recordId = ".";
                        }
                    }

                    writer.Write(recordId);
                    writer.Write('\n');
                    writer.Write(currentRecord.Content);
                    writer.Write("\n\n");
                    recordIdx++;
                }
            }

            file.Dirty = false;
            using(LockImport()) {
                AssetDatabase.ImportAsset(file.AssetPath, ImportAssetOptions.ForceUpdate);
            }
            Debug.LogFormat("[EditorLocDB] Re-exported '{0}' to '{1}' with changes", file.Name, file.AssetPath);
            return true;
        }

        static private uint KeyHash(string key) {
            return new StringHash32(key).HashValue;
        }

        static internal void WriteConstsFile(string[] ids, string className, TextWriter baseWriter) {
            string ns = null;
            string cl = className;
            int lastDot = className.LastIndexOf('.');
            if (lastDot >= 0) {
                ns = className.Substring(0, lastDot);
                cl = className.Substring(lastDot + 1);
            }

            using(IndentedTextWriter writer = new IndentedTextWriter(baseWriter, "    ")) {
                writer.NewLine = "\n";

                writer.WriteLine("// Localization constants");
                writer.Write("// Exported on ");
                writer.WriteLine(DateTime.Now.ToString());
                writer.WriteLine("// Do not manually edit");

                writer.WriteLine("\nusing FDLocalization;");
                writer.WriteLine("using BeauUtil;");
                writer.WriteLine();
                
                if (!string.IsNullOrEmpty(ns)) {
                    writer.Write("namespace ");
                    writer.Write(ns);
                    writer.WriteLine(" {");
                    writer.Indent++;
                }

                writer.Write("public static class ");
                writer.Write(cl);
                writer.WriteLine(" {");
                writer.Indent++;

                foreach(var id in ids) {
                    string niceId = ObjectNames.NicifyVariableName(id).Replace('-', '_').Replace(" ", "").Replace(".", "_");
                    uint hash = new StringHash32(id).HashValue;
                    writer.Write("public static readonly LocId ");
                    writer.Write(niceId);
                    writer.Write(" = new LocId(new StringHash32(0x");
                    writer.Write(hash.ToString("X8"));
                    writer.WriteLine("));");
                }

                writer.Indent--;
                writer.WriteLine("}");

                if (!string.IsNullOrEmpty(ns)) {
                    writer.Indent--;
                    writer.WriteLine(")");
                }
            }
        }

        static private ImportLockSentinel LockImport() {
            s_ImportLocked = true;
            return new ImportLockSentinel();
        }

        private struct ImportLockSentinel : IDisposable {
            public void Dispose() {
                s_ImportLocked = false;
            }
        }

        #endregion // Operations

        #region Menu

        [MenuItem("Field Day/Localization/Reload All")]
        static private void Menu_ForceRebuild() {
            GetInstance().ReloadFiles();
        }

        [MenuItem("Field Day/Localization/Write Changes")]
        static private void Menu_WriteChanges() {
            WriteAnyChanges();
        }

        [MenuItem("Field Day/Localization/Export Consts")]
        static private void Menu_ExportConsts() {
            GetInitialized().TryWriteConsts();
        }

        #endregion // Menu
    
        #region Popup

        private class ExportWizard : ScriptableWizard {
            public string ClassName = "LocalizationConsts";
            [HideInInspector] public string[] Ids;
            [HideInInspector] public string LastPath; 

            void OnWizardUpdate() {
                this.isValid = !string.IsNullOrEmpty(ClassName);
            }

            void OnWizardCreate() {
                string filePath = EditorUtility.SaveFilePanelInProject("Save code file", ClassName + ".cs", "cs", "Select a location to save your code file", LastPath);
                if (!string.IsNullOrEmpty(filePath)) {
                    using(var writer = new StreamWriter(File.Open(filePath, FileMode.Create))) {
                        WriteConstsFile(Ids, ClassName, writer);
                    }
                    var instance = GetInstance();
                    instance.m_LastExportClassName = ClassName;
                    instance.m_LastExportPath = filePath;
                    AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        #endregion // Popup
    }
}