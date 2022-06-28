using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace EasyBugReporter {
    /// <summary>
    /// API for performing dumps and bug reports.
    /// </summary>
    public sealed class BugReporter {

        private const int MaxQueueSize = 8;

        static private readonly HtmlDumpWriter DefaultHtmlWriter = new HtmlDumpWriter();
        static private readonly TextDumpWriter DefaultTextWriter = new TextDumpWriter();

        #region Updates

        static private GameObject s_HookGO;
        static private bool s_HookInitialized;

        static private WorkQueue<DumpWork> s_WorkQueue = new WorkQueue<DumpWork>(MaxQueueSize);
        static private readonly Queue<Action> s_OnEndOfFrameQueue = new Queue<Action>();
        static private StringBuilder s_InMemoryBuilder;

        /// <summary>
        /// Queues the given callback to occur at the end of the current frame.
        /// </summary>
        static public void OnEndOfFrame(Action action) {
            s_OnEndOfFrameQueue.Enqueue(action);
            EnsureHook();
        }

        static private void DispatchEndOfFrame() {
            while(s_OnEndOfFrameQueue.Count > 0) {
                s_OnEndOfFrameQueue.Dequeue()();
            }
        }

        private sealed class HostComponent : MonoBehaviour {
            private Coroutine m_EndOfFrameTick;

            static private readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();

            private void Awake() {
                m_EndOfFrameTick = StartCoroutine(EndOfFrameLoop());
            }

            private void OnDisable() {
                DestroyHook();
            }

            private void LateUpdate() {
                UpdateDumpWork();
            }

            private IEnumerator EndOfFrameLoop() {
                while(true) {
                    yield return EndOfFrame;
                    DispatchEndOfFrame();
                }
            }
        }

        static private void EnsureHook() {
            if (s_HookInitialized) {
                return;
            }

            s_HookInitialized = true;

            if (!s_HookGO) {
                s_HookGO = new GameObject("[BugReporter]");
                s_HookGO.hideFlags = HideFlags.DontSave;
                GameObject.DontDestroyOnLoad(s_HookGO);
                s_HookGO.AddComponent<HostComponent>();
            }
        }

        static private void DestroyHook() {
            if (!s_HookInitialized) {
                return;
            }

            s_HookInitialized = false;
            if (s_HookGO != null) {
                GameObject.Destroy(s_HookGO);
                s_HookGO = null;
            }
        }

        #endregion // Updates

        #region Dump

        private struct DumpWork {
            public DumpSourceCollection Collection;
            public string Title;
            public DateTime Timestamp;
            public DumpFlags Flags;
            public DumpWorkPhase Progress;
            public IDumpSource[] Sources;
            public string TargetPath;
            public ulong SourceWrittenMask;
            public IDumpWriter Writer;
            public Action<DumpResult> OnCompleted;
        }

        private enum DumpWorkPhase {
            Freeze,
            Gather,
            Done
        }

        #region Work

        static private void UpdateDumpWork() {
            if (s_WorkQueue.Count == 0) {
                return;
            }

            ref DumpWork item = ref s_WorkQueue.Peek();

            if (item.Progress == DumpWorkPhase.Freeze) {
                item.Timestamp = DateTime.Now;
                item.Collection.Freeze();
                BeginWriting(ref item);
                item.Writer.Prelude(item.Title, item.Timestamp);
                item.Progress++;
            } else if (item.Progress == DumpWorkPhase.Gather) {
                ulong maxMask = ((ulong) 1 << item.Sources.Length) - 1;
                if (item.SourceWrittenMask != maxMask) {
                    for(int i = 0; i < item.Sources.Length; i++) {
                        ulong mask = (ulong) 1 << i;
                        if ((item.SourceWrittenMask & mask) == mask) {
                            continue;
                        }

                        if (item.Sources[i].Dump(item.Writer)) {
                            item.SourceWrittenMask |= mask;
                        }
                    }
                }

                if (item.SourceWrittenMask == maxMask) {
                    item.Progress++;
                    DumpResult result = EndWriting(ref item);
                    item.Collection.Unfreeze();
                    item.OnCompleted?.Invoke(result);
                    PresentReport(result);
                    s_WorkQueue.Dequeue();
                }
            }
        }

        static private void BeginWriting(ref DumpWork item) {
            if ((item.Flags & DumpFlags.InMemory) != 0) {
                if (s_InMemoryBuilder == null) {
                    s_InMemoryBuilder = new StringBuilder(1024);
                } else {
                    s_InMemoryBuilder.Length = 0;
                }
                StringWriter writer = new StringWriter(s_InMemoryBuilder);
                item.Writer.Begin(writer);
            } else {
                Directory.CreateDirectory(Path.GetDirectoryName(item.TargetPath));
                FileStream file = File.Open(item.TargetPath, FileMode.Create);
                item.Writer.Begin(file);
            }
        }

        static private DumpResult EndWriting(ref DumpWork item) {
            DumpResult result;
            if ((item.Flags & DumpFlags.InMemory) != 0) {
                result.Contents = s_InMemoryBuilder.ToString();
                s_InMemoryBuilder.Length = 0;
                result.Url = result.FilePath = null;
            } else {
                result.Contents = null;
                result.FilePath = item.TargetPath;
                result.Url = PathToURL(item.TargetPath);
            }
            item.Writer.End();
            item.Writer = null;
            return result;
        }

        static private void PresentReport(DumpResult result) {
            if (result.Url != null) {
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.OpenWithDefaultApp(result.FilePath);
                #else
                Application.OpenURL(result.Url);
                #endif // UNITY_EDITOR
            } else {
                #if UNITY_WEBGL && !UNITY_EDITOR
                BugReporter_DisplayDocument(result.Contents);
                #endif // UNITY_WEBGL
            }
        }

        #endregion // Work

        #region Setup

        /// <summary>
        /// Dumps context in its default format (HTML) and attempts to display it to the user.
        /// </summary>
        static public void DumpContext(DumpSourceCollection collection) {
            DumpContext(collection, DefaultHtmlWriter, null, 0, DumpFormat.Html, null);
        }

        static internal void DumpContext(DumpSourceCollection collection, IDumpWriter writer, string title, DumpFlags flags, DumpFormat format, Action<DumpResult> onCompleted) {
            DumpWork workItem;
            workItem.Collection = collection;

            IDumpSource[] sources = new IDumpSource[collection.Count];
            collection.CopyTo(sources, 0);
            workItem.Sources = sources;
            workItem.SourceWrittenMask = 0u;
            workItem.Writer = writer;
            workItem.OnCompleted = onCompleted;
            workItem.Progress = DumpWorkPhase.Freeze;
            workItem.Title = string.IsNullOrEmpty(title) ? Application.productName : title;
            workItem.Timestamp = default;
            workItem.Flags = GetPlatformFlags(flags);
            workItem.TargetPath = GetPlatformPath(workItem.Title, workItem.Flags, format);

            s_WorkQueue.Enqueue(workItem);

            EnsureHook();
        }

        static private string GetPlatformPath(string title, DumpFlags flags, DumpFormat format) {
            if ((flags & DumpFlags.InMemory) != 0) {
                return null;
            }

            string fileName = string.Format("{0} {1}", title, DateTime.Now.ToString("dd-MM-yyyy-HHmmss"));
            string extension = ".txt";
            if (format == DumpFormat.Html) {
                extension = ".html";
            }

            #if UNITY_EDITOR
            return "Reports/" + fileName + extension;
            #else
            return Application.persistentDataPath + "/Reports/" + fileName + extension;
            #endif // UNITY_EDITOR
        }

        static private DumpFlags GetPlatformFlags(DumpFlags flags) {
            #if UNITY_WEBGL && !UNITY_EDITOR
            flags |= GatherContextFlags.InMemory;
            #endif // UNITY_WEBGL
            return flags;
        }

        #endregion // Setup

        /// <summary>
        /// Configuration flags for a data dump.
        /// </summary>
        [Flags]
        public enum DumpFlags {
            InMemory = 0x01,
        }

        /// <summary>
        /// Result of a data dump.
        /// </summary>
        public struct DumpResult {
            public string Url;
            public string FilePath;
            public string Contents;
        }

        #endregion // Context Gather

        #region Utils

        static private string PathToURL(string path) {
            switch (Application.platform) {
                case RuntimePlatform.Android:
                case RuntimePlatform.WebGLPlayer:
                    return path;

                case RuntimePlatform.WSAPlayerARM:
                case RuntimePlatform.WSAPlayerX64:
                case RuntimePlatform.WSAPlayerX86:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    return "file:///" + path;

                default:
                    return "file://" + path;
            }
        }

        [DllImport("__Internal")]
        static private extern void BugReporter_DisplayDocument(string documentSrc);

        #endregion // Utils
    }
}