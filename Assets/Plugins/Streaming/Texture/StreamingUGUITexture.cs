#if UNITY_2018_3_OR_NEWER
#define USE_ALWAYS
#endif // UNITY_2018_3_OR_NEWER

using System;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.EventSystems;
#endif // UNITY_EDITOR

namespace StreamingAssets {
    #if USE_ALWAYS
    [ExecuteAlways]
    #else
    [ExecuteInEditMode]
    #endif // USE_ALWAYS
    [AddComponentMenu("Streaming Assets/Streaming UGUI Texture")]
    [RequireComponent(typeof(RawImage), typeof(RectTransform))]
    public sealed class StreamingUGUITexture : UIBehaviour, IStreamingComponent, ILayoutSelfController {
        #region Inspector

        [SerializeField] private RawImage m_RawImage;

        [SerializeField, StreamingImagePath] private string m_Path;
        [SerializeField] private AutoSizeMode m_AutoSize = AutoSizeMode.Disabled;

        #endregion // Inspector

        [NonSerialized] private Texture2D m_LoadedTexture;
        private DrivenRectTransformTracker m_Tracker;
 
        private readonly Streaming.AssetCallback OnAssetUpdated;

        private StreamingUGUITexture() {
            OnAssetUpdated = (StringHash32 id, Streaming.AssetStatus status, object asset) => {
                if (status == Streaming.AssetStatus.Loaded) {
                    Resize(m_AutoSize);
                }
            };
        }

        #region Properties

        /// <summary>
        /// Path or URL to the texture.
        /// </summary>
        public string Path {
            get { return m_Path; }
            set {
                if (m_Path != value) {
                    m_Path = value;
                    if (isActiveAndEnabled) {
                        LoadTexture();
                    }
                }
            }
        }

        /// <summary>
        /// Returns if the texture is fully loaded.
        /// </summary>
        public bool IsLoaded() {
            return Streaming.IsLoaded(m_LoadedTexture);
        }

        /// <summary>
        /// Returns if the texture is currently loading.
        /// </summary>
        public bool IsLoading() {
            return (Streaming.Status(m_LoadedTexture) & Streaming.AssetStatus.PendingLoad) != 0;;
        }

        /// <summary>
        /// Color of the renderer.
        /// </summary>
        public Color Color {
            get { return m_RawImage.color; }
            set { m_RawImage.color = Color; }
        }

        /// <summary>
        /// Transparency of the renderer.
        /// </summary>
        public float Alpha {
            get { return m_RawImage.color.a; }
            set {
                var rawColor = m_RawImage.color;
                if (rawColor.a != value) {
                    rawColor.a = value;
                    m_RawImage.color = rawColor;
                }
            }
        }

        #endregion // Properties

        /// <summary>
        /// Resizes the mesh to preserve aspect ratio.
        /// </summary>
        public void Resize(AutoSizeMode sizeMode) {
            if (sizeMode == AutoSizeMode.Disabled || !m_LoadedTexture) {
                m_Tracker.Clear();
                return;
            }

            RectTransform rect = (RectTransform) transform;
            Vector2 size = rect.rect.size;

            m_Tracker.Clear();

            switch(sizeMode) {
                case AutoSizeMode.StretchX: {
                    m_Tracker.Add(this, rect, DrivenTransformProperties.SizeDeltaX);
                    break;
                }
                case AutoSizeMode.StretchY: {
                    m_Tracker.Add(this, rect, DrivenTransformProperties.SizeDeltaY);
                    break;
                }
                case AutoSizeMode.Fit:
                case AutoSizeMode.Fill: {
                    m_Tracker.Add(this, rect, DrivenTransformProperties.SizeDelta);
                    break;
                }
            }

            if (!StreamingHelper.AutoSize(sizeMode, m_LoadedTexture, m_RawImage.uvRect, ref size, StreamingHelper.GetParentSize(rect))) {
                return;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                UnityEditor.Undo.RecordObject(rect, "Changing size");
                UnityEditor.EditorUtility.SetDirty(rect);
            }
            #endif // UNITY_EDITOR

            switch(sizeMode) {
                case AutoSizeMode.StretchX: {
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                    break;
                }
                case AutoSizeMode.StretchY: {
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                    break;
                }
                case AutoSizeMode.Fit:
                case AutoSizeMode.Fill: {
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                    break;
                }
            }

        }

        #region Unity Events

        protected override void OnEnable() {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                if (EditorApplication.isPlayingOrWillChangePlaymode) {
                    return;
                }
                
                m_RawImage = GetComponent<RawImage>();
                LoadTexture();
                return;
            }
            #endif // UNITY_EDITOR

            LoadTexture();
        }

        protected override void OnDisable() {
            m_Tracker.Clear();
            Unload();
        }

        protected override void OnDestroy() {
            Unload();
        }

        protected override void OnRectTransformDimensionsChange() {
            Resize(m_AutoSize);
        }

        #if UNITY_EDITOR

        [NonSerialized] private Vector2 m_CachedParentSize;

        private void Update() {
            if (!Application.IsPlaying(this)) {
                if (EditorApplication.isPlayingOrWillChangePlaymode) {
                    return;
                }

                Vector2? parentSize = StreamingHelper.GetParentSize(transform);
                if (parentSize.HasValue && Ref.Replace(ref m_CachedParentSize, parentSize.Value)) {
                    Resize(m_AutoSize);
                }
            }
        }

        #endif // UNITY_EDITOR

        #endregion // Unity Events

        #region Resources

        /// <summary>
        /// Prefetches
        /// </summary>
        public void Prefetch() {
            LoadTexture();
        }

        private void LoadTexture() {
            if (!Streaming.Texture(m_Path, ref m_LoadedTexture, OnAssetUpdated)) {
                if (!m_LoadedTexture) {
                    m_RawImage.enabled = false;
                }
                return;
            }

            m_RawImage.enabled = m_LoadedTexture;
            m_RawImage.texture = m_LoadedTexture;

            if (Streaming.IsLoaded(m_LoadedTexture))
                Resize(m_AutoSize);
        }

        /// <summary>
        /// Unloads all resources owned by the StreamingWorldTexture.
        /// </summary>
        public void Unload() {
            if (m_RawImage) {
                m_RawImage.enabled = false;
            }

            Streaming.Unload(ref m_LoadedTexture);
        }

        #endregion // Resources

        #region ILayoutSelfController

        void ILayoutController.SetLayoutHorizontal() {
            if (StreamingHelper.IsAutoSizeHorizontal(m_AutoSize)) {
                Resize(m_AutoSize);
            }
        }

        void ILayoutController.SetLayoutVertical() {
            if (StreamingHelper.IsAutoSizeVertical(m_AutoSize)) {
                Resize(m_AutoSize);
            }
        }

        #endregion // ILayoutSelfController

        #region Editor

        #if UNITY_EDITOR

        protected override void Reset() {
            m_RawImage = GetComponent<RawImage>();
        }

        protected override void OnValidate() {
            if (EditorApplication.isPlaying || PrefabUtility.IsPartOfPrefabAsset(this)) {
                return;
            }

            m_RawImage = GetComponent<RawImage>();

            EditorApplication.delayCall += () => {
                if (!this) {
                    return;
                }

                LoadTexture();
                Resize(m_AutoSize);
            };
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}