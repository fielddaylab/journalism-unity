#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;

[assembly: InternalsVisibleTo("StreamingAssets.Editor")]

namespace StreamingAssets {
    #if UNITY_EDITOR
    public class Streaming : UnityEditor.AssetPostprocessor {
    #else
    static public class Streaming {
    #endif // UNITY_EDITOR

        static private readonly Color32[] TextureLoadingBytes = new Color32[] {
            Color.black, Color.white, Color.white, Color.black
        };

        #region Editor Hooks

        #if UNITY_EDITOR

        [UnityEditor.InitializeOnLoadMethod]
        static private void EditorInitialize() {
            UnityEditor.EditorApplication.playModeStateChanged += PlayModeStateChange;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
            UnityEditor.Experimental.SceneManagement.PrefabStage.prefabStageClosing += OnPrefabStageClosing;
            AppDomain.CurrentDomain.DomainUnload += (e, o) => UnloadAll();
        }

        static private void PlayModeStateChange(UnityEditor.PlayModeStateChange stateChange) {
            if (stateChange != UnityEditor.PlayModeStateChange.ExitingEditMode && stateChange != UnityEditor.PlayModeStateChange.ExitingPlayMode) {
                return;
            }

            UnloadAll();
        }

        static private void OnPrefabStageClosing(UnityEditor.Experimental.SceneManagement.PrefabStage _) {
            UnityEditor.EditorApplication.delayCall += UnloadUnusedSync;
        }

        static private void OnSceneOpened(UnityEngine.SceneManagement.Scene _, UnityEditor.SceneManagement.OpenSceneMode __) {
            if (UnityEditor.EditorApplication.isPlaying) {
                return;
            }
            
            UnloadUnusedSync();
        }

        #endif // UNITY_EDITOR

        #endregion // Editor Hooks

        #region Types

        public delegate void AssetCallback(StringHash32 id, AssetStatus status, object asset);

        private enum AssetType : ushort {
            Texture,
            Audio
        }

        public enum AssetStatus : byte {
            Unloaded = 0,
            Invalid = 0x01,

            PendingUnload = 0x02,
            PendingLoad = 0x04,
            Loaded = 0x08,
            Error = 0x10,
        }

        private class AssetMeta {
            public AssetType Type;
            public AssetStatus Status;
            public ushort RefCount;
            public long Size;
            public long LastModifiedTS;

            public UnityWebRequest Loader;
            #if UNITY_EDITOR
            public HotReloadableFileProxy Proxy;
            #endif // UNITY_EDITOR

            public RingBuffer<AssetCallback> OnUpdate;
        }

        #endregion // Types

        static private readonly Dictionary<StringHash32, Texture2D> s_Textures = new Dictionary<StringHash32, Texture2D>();
        static private readonly Dictionary<StringHash32, AudioClip> s_AudioClips = new Dictionary<StringHash32, AudioClip>();
        static private readonly Dictionary<StringHash32, AssetMeta> s_Metas = new Dictionary<StringHash32, AssetMeta>();
        static private readonly Dictionary<int, StringHash32> s_ReverseLookup = new Dictionary<int, StringHash32>();
        static private readonly RingBuffer<StringHash32> s_UnloadQueue = new RingBuffer<StringHash32>();
        static private uint s_LoadCount = 0;
        static private AsyncHandle s_LoadHandle;
        static private AsyncHandle s_UnloadHandle;
        static private long s_TextureMemoryUsage = 0;

        #if UNITY_EDITOR
        static private readonly HotReloadBatcher s_Batcher = new HotReloadBatcher();
        #endif // UNITY_EDITOR

        #region Textures

        /// <summary>
        /// Loads a texture from a given url.
        /// Returns if the "texture" parameter has changed.
        /// </summary>
        static public bool Texture(string pathOrUrl, ref Texture2D texture, AssetCallback callback = null) {
            if (string.IsNullOrEmpty(pathOrUrl)) {
                return Unload(ref texture, callback);
            }

            StringHash32 id = pathOrUrl;
            Texture2D loadedTexture;
            AssetMeta meta = GetTextureMeta(id, pathOrUrl, out loadedTexture);

            if (texture != loadedTexture) {
                Dereference(texture, callback);
                texture = loadedTexture;
                meta.RefCount++;
                meta.LastModifiedTS = CurrentTimestamp();
                meta.Status &= ~AssetStatus.PendingUnload;
                s_UnloadQueue.FastRemove(id);
                AddCallback(meta, id, texture, callback);
                return true;
            }

            return false;
        }

        static private AssetMeta GetTextureMeta(StringHash32 id, string pathOrUrl, out Texture2D texture) {
            Texture2D loadedTexture;
            AssetMeta meta;
            if (!s_Metas.TryGetValue(id, out meta)) {
                meta = new AssetMeta();

                Log.Msg( "[Streaming] Loading streamed texture '{0}'...", id);
                
                meta.Type = AssetType.Texture;
                meta.Status = AssetStatus.PendingLoad;
                #if UNITY_EDITOR
                if (!UnityEditor.EditorApplication.isPlaying) {
                    loadedTexture = LoadTexture_Editor(id, pathOrUrl, meta);
                    s_Batcher.Add(meta.Proxy);
                } else
                #endif // UNITY_EDITOR
                {
                    loadedTexture = LoadTextureAsync(id, pathOrUrl, meta);
                }

                s_Metas[id] = meta;
                s_Textures[id] = loadedTexture;
                s_ReverseLookup[loadedTexture.GetInstanceID()] = id;
            } else {
                loadedTexture = s_Textures[id];
            }

            texture = loadedTexture;
            return meta;
        }

        /// <summary>
        /// Dereferences the given texture.
        /// </summary>
        static public bool Unload(ref Texture2D texture, AssetCallback callback = null) {
            if (!texture.IsReferenceNull()) {
                Dereference(texture, callback);
                texture = null;
                return true;
            }

            return false;
        }

        #if UNITY_EDITOR

        static private Texture2D LoadTexture_Editor(StringHash32 id, string pathOrUrl, AssetMeta meta) {
            if (IsURL(pathOrUrl)) {
                Log.Error("[Streaming] Cannot load texture from URL when not in playmode '{0}'", pathOrUrl);
                Log.Error("[Streaming] Failed to load texture from '{0}'", pathOrUrl);
                Texture2D texture = CreatePlaceholderTexture(pathOrUrl, true);
                meta.Proxy = null;
                meta.Status = AssetStatus.Error;
                RecomputeSize(ref s_TextureMemoryUsage, meta, texture);
                return texture;
            }

            string correctedPath = StreamingPath(pathOrUrl);
            if (File.Exists(correctedPath)) {
                byte[] bytes = File.ReadAllBytes(correctedPath);
                Texture2D texture = new Texture2D(1, 1);
                texture.name = pathOrUrl;
                texture.hideFlags = HideFlags.DontSave;
                texture.filterMode = GetTextureFilterMode(pathOrUrl);
                texture.wrapMode = GetTextureWrapMode(pathOrUrl);
                texture.LoadImage(bytes, false);
                Log.Msg("[Streaming] ...finished loading (sync) '{0}'", id);
                meta.Proxy = new HotReloadableFileProxy(correctedPath, (p, s) => {
                    if (s == HotReloadOperation.Modified) {
                        texture.LoadImage(File.ReadAllBytes(p), false);
                        RecomputeSize(ref s_TextureMemoryUsage, meta, texture);
                        Log.Msg("[Streaming] Texture '{0}' reloaded", id);
                        InvokeCallbacks(meta, id, texture);
                    } else {
                        texture.filterMode = FilterMode.Point;
                        texture.Resize(2, 2);
                        texture.SetPixels32(TextureLoadingBytes);
                        texture.Apply(false, true);
                        RecomputeSize(ref s_TextureMemoryUsage, meta, texture);

                        meta.Status = AssetStatus.Error;
                        Log.Msg("[Streaming] Texture '{0}' was deleted", id);
                        InvokeCallbacks(meta, id, texture);
                    }
                });
                meta.Status = AssetStatus.Loaded;
                return texture;
            } else {
                Log.Error("[Streaming] Failed to load texture from '{0}' - file does not exist", pathOrUrl);
                Texture2D texture = CreatePlaceholderTexture(pathOrUrl, true);
                meta.Proxy = null;
                meta.Status = AssetStatus.Error;
                RecomputeSize(ref s_TextureMemoryUsage, meta, texture);
                return texture;
            }
        }

        #endif // UNITY_EDITOR

        static private Texture2D LoadTextureAsync(StringHash32 id, string pathOrUrl, AssetMeta meta) {
            Texture2D texture = CreatePlaceholderTexture(pathOrUrl, false);
            string url = ResolvePathToURL(pathOrUrl);
            var request = meta.Loader = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerBuffer();
            Async.InvokeAsync(() => {
                var sent = request.SendWebRequest();
                sent.completed += (r) => HandleTextureUWRFinished(id, pathOrUrl, meta, request);
            });
            RecomputeSize(ref s_TextureMemoryUsage, meta, texture);
            s_LoadCount++;
            return texture;
        }

        static private void HandleTextureUWRFinished(StringHash32 id, string pathOrUrl, AssetMeta meta, UnityWebRequest request) {
            s_LoadCount--;

            if (meta.Status == AssetStatus.Unloaded) {
                return;
            }

            if ((meta.Status & AssetStatus.PendingUnload) != 0) {
                UnloadSingle(id, 0, 0);
                s_UnloadQueue.FastRemove(id);
                return;
            }

            if (request.isNetworkError || request.isHttpError) {
                OnTextureDownloadFail(id, pathOrUrl, meta);
            } else {
                OnTextureDownloadCompleted(id, request.downloadHandler.data, pathOrUrl, meta);
            }

            request.Dispose();
            meta.Loader = null;
        }

        static private void OnTextureDownloadCompleted(StringHash32 id, byte[] source, string pathOrUrl, AssetMeta meta) {
            Texture2D dest = s_Textures[id];
            try {
                dest.LoadImage(source, true);
                dest.filterMode = GetTextureFilterMode(pathOrUrl);
                dest.wrapMode = GetTextureWrapMode(pathOrUrl);
            } catch(Exception e) {
                UnityEngine.Debug.LogException(e);
                OnTextureDownloadFail(id, pathOrUrl, meta);
                return;
            }

            meta.Status = AssetStatus.Loaded;
            RecomputeSize(ref s_TextureMemoryUsage, meta, dest);
            Log.Msg("[Streaming] ...finished loading (async) '{0}'", id);
            InvokeCallbacks(meta, id, dest);
        }

        static private void OnTextureDownloadFail(StringHash32 id, string pathOrUrl, AssetMeta meta) {
            Log.Error("[Streaming] Failed to load texture '{0}' from '{1}", id, pathOrUrl);
            meta.Loader = null;
            meta.Status = AssetStatus.Error;
            InvokeCallbacks(meta, id, s_Textures[id]);
        }

        static private FilterMode GetTextureFilterMode(string url) {
            if (url.Contains("[pt]")) {
                return FilterMode.Point;
            } else {
                return FilterMode.Bilinear;
            }
        }

        static private TextureWrapMode GetTextureWrapMode(string url) {
            if (url.Contains("[wrap]")) {
                return TextureWrapMode.Repeat;
            } else {
                return TextureWrapMode.Clamp;
            }
        }

        static private Texture2D CreatePlaceholderTexture(string name, bool final) {
            Texture2D texture;
            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.name = name;
            texture.SetPixels32(TextureLoadingBytes);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, final);
            texture.hideFlags = HideFlags.DontSave;
            return texture;
        }

        #endregion // Textures

        #region Paths

        static private bool IsURL(string pathOrUrl) {
            return pathOrUrl.Contains("://");
        }

        static private string StreamingPath(string relativePath) {
            return Path.Combine(Application.streamingAssetsPath, relativePath);
        }

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

        /// <summary>
        /// Converts a path relative to StreamingAssets into a URL.
        /// If the input is already a URL, it is preserved.
        /// </summary>
        static public string ResolvePathToURL(string relativePath) {
            if (IsURL(relativePath)) {
                return relativePath;
            }
            return PathToURL(StreamingPath(relativePath));
        }

        #endregion // Paths

        #region Management

        /// <summary>
        /// Attempts to return the streaming id associated with the given asset.
        /// </summary>
        static public bool TryGetId(UnityEngine.Object instance, out StringHash32 id) {
            if (!instance) {
                id = default;
                return false;
            }

            int instanceId = instance.GetInstanceID();
            if (!s_ReverseLookup.TryGetValue(instanceId, out id)) {
                Log.Warn("[Streaming] No asset metadata found for {0}'", instance);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns if any loads are currently executing.
        /// </summary>
        static public bool IsLoading() {
            return s_LoadCount > 0;
        }

        /// <summary>
        /// Returns if the asset with the given streaming id is loaded.
        /// </summary>
        static public bool IsLoaded(StringHash32 id) {
            return !id.IsEmpty && Status(id) == AssetStatus.Loaded;
        }

        /// <summary>
        /// Returns if the given streaming asset is loaded.
        /// </summary>
        static public bool IsLoaded(UnityEngine.Object instance) {
            return instance && Status(instance) == AssetStatus.Loaded;
        }

        /// <summary>
        /// Returns the status of the asset with the given streaming id.
        /// </summary>
        static public AssetStatus Status(StringHash32 id) {
            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                return meta.Status;
            }

            return AssetStatus.Unloaded;
        }

        /// <summary>
        /// Returns the status of the given streaming asset.
        /// </summary>
        static public AssetStatus Status(UnityEngine.Object instance) {
            StringHash32 id;
            if (!TryGetId(instance, out id)) {
                return AssetStatus.Invalid;
            }

            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                return meta.Status;
            }

            return AssetStatus.Unloaded;
        }

        static private long CurrentTimestamp() {
            return Stopwatch.GetTimestamp();
        }

        #if UNITY_EDITOR

        static private void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
            if (Application.isPlaying)
                return;
            
            s_Batcher.TryReloadAll();
        }

        #endif // UNITY_EDITOR

        #endregion // Management

        #region Unloading

        #if UNITY_EDITOR

        static private void UnloadUnusedSync() {
            IdentifyUnusedPrefetchSync();

            StringHash32 id;
            while(s_UnloadQueue.TryPopFront(out id)) {
                UnloadSingle(id, 0, 0);
            }
        }

        static private void IdentifyUnusedPrefetchSync() {
            AssetMeta meta;
            foreach(var metaKV in s_Metas) {
                meta = metaKV.Value;
                if (meta.RefCount == 0 && (meta.Status & AssetStatus.PendingUnload) == 0) {
                    meta.Status |= AssetStatus.PendingUnload;
                    s_UnloadQueue.PushBack(metaKV.Key);
                }
            }
        }

        #endif // UNITY_EDITOR

        static private bool Dereference(UnityEngine.Object instance, AssetCallback callback) {
            StringHash32 id;
            if (!TryGetId(instance, out id)) {
                return false;
            }

            AssetMeta meta;
            if (s_Metas.TryGetValue(id, out meta)) {
                if (meta.RefCount > 0) {
                    meta.RefCount--;
                    meta.LastModifiedTS = CurrentTimestamp();
                    RemoveCallback(meta, callback);
                    if (meta.RefCount == 0) {
                        meta.Status |= AssetStatus.PendingUnload;
                        s_UnloadQueue.PushBack(id);
                    }
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns if streaming assets are currently unloading.
        /// </summary>
        static public bool IsUnloading() {
            return s_UnloadHandle.IsRunning();
        }

        /// <summary>
        /// Unloads all unused streaming assets asynchronously.
        /// </summary>
        static public AsyncHandle UnloadUnusedAsync() {
            if (s_UnloadHandle.IsRunning()) {
                return s_UnloadHandle;
            }
            return s_UnloadHandle = Async.Schedule(UnloadUnusedAsyncJob(0), AsyncFlags.MainThreadOnly);
        }

        /// <summary>
        /// Unloads old unused streaming assets asynchronously.
        /// </summary>
        static public AsyncHandle UnloadUnusedAsync(float minAge) {
            if (s_UnloadHandle.IsRunning()) {
                return s_UnloadHandle;
            }
            long minAgeInTicks = (long) (minAge * TimeSpan.TicksPerSecond);
            return s_UnloadHandle = Async.Schedule(UnloadUnusedAsyncJob(minAgeInTicks), AsyncFlags.MainThreadOnly);
        }

        static private IEnumerator UnloadUnusedAsyncJob(long deleteThreshold) {
            StringHash32 id;
            long current = CurrentTimestamp();
            while(s_UnloadQueue.TryPopFront(out id)) {
                UnloadSingle(id, current, deleteThreshold);
                yield return null;
            }

            s_UnloadHandle = default;
        }

        static internal void UnloadAll() {
            foreach(var texture in s_Textures.Values) {
                DestroyResource(texture);
            }
            foreach(var audio in s_AudioClips.Values) {
                DestroyResource(audio);
            }

            foreach(var meta in s_Metas.Values) {
                meta.Status = AssetStatus.Unloaded;
                if (meta.Loader != null) {
                    meta.Loader.Dispose();
                    meta.Loader = null;
                }
            }

            s_ReverseLookup.Clear();
            s_Textures.Clear();
            s_AudioClips.Clear();
            s_Metas.Clear();
            s_UnloadQueue.Clear();

            #if UNITY_EDITOR
            s_Batcher.Dispose();
            #endif // UNITY_EDITOR

            s_TextureMemoryUsage = 0;

            Log.Msg("[Streaming] Unloaded all streamed assets");
        }
    
        static internal bool UnloadSingle(StringHash32 id, long now, long deleteThreshold = 0) {
            AssetMeta meta = s_Metas[id];
            UnityEngine.Object resource = null;
            if (meta.RefCount > 0) {
                return false;
            }

            if (deleteThreshold > 0 && (now - meta.LastModifiedTS) < deleteThreshold) {
                return false;
            }

            s_Metas.Remove(id);
            if (meta.Loader != null) {
                meta.Loader.Dispose();
                meta.Loader = null;
            }

            #if UNITY_EDITOR
            if (meta.Proxy != null) {
                s_Batcher.Remove(meta.Proxy);
                meta.Proxy.Dispose();
            }
            #endif // UNITY_EDITOR

            if (meta.OnUpdate != null) {
                meta.OnUpdate.Clear();
                meta.OnUpdate = null;
            }

            if ((meta.Status & (AssetStatus.PendingLoad | AssetStatus.Loaded | AssetStatus.Error)) != 0) {
                switch(meta.Type) {
                    case AssetType.Texture: {
                            resource = s_Textures[id];
                            s_Textures.Remove(id);
                            s_TextureMemoryUsage -= meta.Size;
                            break;
                        }

                    case AssetType.Audio: {
                            resource = s_AudioClips[id];
                            
                            s_AudioClips.Remove(id);
                            break;
                        }
                }
                s_ReverseLookup.Remove(resource.GetInstanceID());
                DestroyResource(resource);
                resource = null;
            }

            Log.Msg("[Streaming] Unloaded streamed asset '{0}'", id);
            
            meta.Size = 0;
            meta.Status = AssetStatus.Unloaded;

            return true;
        }
        
        #endregion // Unloading
    
        #region Utilities

        static private void AddCallback(AssetMeta meta, StringHash32 id, object asset, AssetCallback callback) {
            if (callback != null) {
                if (meta.OnUpdate == null) {
                    meta.OnUpdate = new RingBuffer<AssetCallback>();
                }
                meta.OnUpdate.PushBack(callback);
                if (meta.Status != AssetStatus.PendingLoad) {
                    callback(id, meta.Status, asset);
                }
            }
        }

        static private void InvokeCallbacks(AssetMeta meta, StringHash32 id, object asset) {
            var onUpdate = meta.OnUpdate;
            AssetStatus status = meta.Status;
            if (onUpdate != null) {
                for(int i = 0, len = onUpdate.Count; i < len; i++) {
                    onUpdate[i](id, status, asset);
                }
            }
        }

        static private void RemoveCallback(AssetMeta meta, AssetCallback callback) {
            if (callback != null && meta.OnUpdate != null) {
                meta.OnUpdate.FastRemove(callback);
            }
        }

        static internal void DestroyResource<T>(ref T resource) where T : UnityEngine.Object {
            if (resource != null) {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEngine.Object.DestroyImmediate(resource);
                } else {
                    UnityEngine.Object.Destroy(resource);
                }
                #else
                UnityEngine.Object.Destroy(resource);
                #endif // UNITY_EDITOR

                resource = null;
            }
        }

        static internal void DestroyResource(UnityEngine.Object resource) {
            if (resource != null) {
                #if UNITY_EDITOR
                if (!Application.isPlaying) {
                    UnityEngine.Object.DestroyImmediate(resource);
                } else {
                    UnityEngine.Object.Destroy(resource);
                }
                #else
                UnityEngine.Object.Destroy(resource);
                #endif // UNITY_EDITOR
            }
        }

        static private void RecomputeSize(ref long memUsage, AssetMeta meta, UnityEngine.Object asset) {
            memUsage -= meta.Size;
            meta.Size = CalculateSize(asset);
            memUsage += meta.Size;

            Log.Msg("[Streaming] Size of texture '{0}' is {1}kB", asset.name, meta.Size / 1024);
        }

        static internal long CalculateSize(UnityEngine.Object resource) {
            if (Profiler.enabled) {
                return Profiler.GetRuntimeMemorySizeLong(resource);
            }

            Texture2D tex = resource as Texture2D;
            if (tex != null) {
                return UnityHelper.CalculateMemoryUsage(tex);
            }

            return 0;
        }

        #endregion // Utilities
    }

    public class StreamingImagePathAttribute : StreamingPathAttribute {
        public StreamingImagePathAttribute()
            : base("png,jpg,jpeg")
        { }
    }

    public class StreamingVideoPathAttribute : StreamingPathAttribute {
        public StreamingVideoPathAttribute()
            : base("webm,mp4")
        { }
    }

    public class StreamingAudioPathAttribute : StreamingPathAttribute {
        public StreamingAudioPathAttribute()
            : base("mp3,ogg,wav,aac")
        { }
    }
}