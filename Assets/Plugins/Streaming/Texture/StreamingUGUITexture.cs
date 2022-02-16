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
        [SerializeField] private ColorGroup m_ColorGroup;

        [SerializeField, StreamingImagePath] private string m_Path;
        [SerializeField] private Rect m_UVRect = new Rect(0f, 0f, 1f, 1f);
        [SerializeField] private AutoSizeMode m_AutoSize = AutoSizeMode.Disabled;

        #endregion // Inspector

        [NonSerialized] private Texture2D m_LoadedTexture;
        [NonSerialized] private Rect m_ClippedUVs;
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
            get { 
                if (m_ColorGroup) {
                    return m_ColorGroup.Color;
                }
                return m_RawImage.color;
            }
            set {
                if (m_ColorGroup) {
                    m_ColorGroup.Color = value;
                } else {
                    m_RawImage.color = value;
                }
            }
        }

        /// <summary>
        /// Transparency of the renderer.
        /// </summary>
        public float Alpha {
            get {
                if (m_ColorGroup) {
                    return m_ColorGroup.GetAlpha();
                }
                return m_RawImage.color.a;
            }
            set {
                if (m_ColorGroup) {
                    m_ColorGroup.SetAlpha(value);
                } else {
                    var rawColor = m_RawImage.color;
                    if (rawColor.a != value) {
                        rawColor.a = value;
                        m_RawImage.color = rawColor;
                    }
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
                if (m_ClippedUVs != m_UVRect) {
                    m_ClippedUVs = m_UVRect;
                    LoadClipping();
                }
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
                case AutoSizeMode.Fill:
                case AutoSizeMode.FillWithClipping: {
                    m_Tracker.Add(this, rect, DrivenTransformProperties.SizeDelta);
                    break;
                }
            }

            StreamingHelper.UpdatedResizeProperty updated = StreamingHelper.AutoSize(sizeMode, m_LoadedTexture, m_UVRect, rect.localPosition, rect.pivot, ref size, ref m_ClippedUVs, StreamingHelper.GetParentSize(rect));
            if (updated == 0) {
                return;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                if ((updated & StreamingHelper.UpdatedResizeProperty.Size) != 0) {
                    UnityEditor.Undo.RecordObject(rect, "Changing size");
                    UnityEditor.EditorUtility.SetDirty(rect);
                }
                if ((updated & StreamingHelper.UpdatedResizeProperty.Clip) != 0) {
                    UnityEditor.Undo.RecordObject(m_RawImage, "Changing clipping");
                    UnityEditor.EditorUtility.SetDirty(m_RawImage);
                }
            }
            #endif // UNITY_EDITOR

            if ((updated & StreamingHelper.UpdatedResizeProperty.Size) != 0) {
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
                    case AutoSizeMode.Fill:
                    case AutoSizeMode.FillWithClipping: {
                        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                        break;
                    }
                }
            }

            if ((updated & StreamingHelper.UpdatedResizeProperty.Clip) != 0) {
                LoadClipping();
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
                m_ColorGroup = GetComponent<ColorGroup>();

                LoadTexture();
                LoadClipping();
                return;
            }
            #endif // UNITY_EDITOR

            LoadTexture();
            LoadClipping();
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
            LoadClipping();
        }

        private void LoadTexture() {
            if (!Streaming.Texture(m_Path, ref m_LoadedTexture, OnAssetUpdated)) {
                if (!m_LoadedTexture) {
                    m_RawImage.enabled = false;
                    if (m_ColorGroup) {
                        m_ColorGroup.Visible = false;
                    }
                }
                return;
            }

            m_RawImage.enabled = m_LoadedTexture;
            m_RawImage.texture = m_LoadedTexture;
            m_ClippedUVs = m_UVRect;

            if (m_ColorGroup) {
                m_ColorGroup.Visible = m_LoadedTexture;
            }

            if (Streaming.IsLoaded(m_LoadedTexture))
                Resize(m_AutoSize);
        }

        private void LoadClipping() {
            m_RawImage.uvRect = m_ClippedUVs;
        }

        /// <summary>
        /// Unloads all resources owned by the StreamingWorldTexture.
        /// </summary>
        public void Unload() {
            if (m_RawImage) {
                m_RawImage.enabled = false;
            }
            if (m_ColorGroup) {
                m_ColorGroup.Visible = false;
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
            m_ColorGroup = GetComponent<ColorGroup>();
        }

        protected override void OnValidate() {
            if (EditorApplication.isPlaying || PrefabUtility.IsPartOfPrefabAsset(this)) {
                return;
            }

            m_RawImage = GetComponent<RawImage>();
            m_ColorGroup = GetComponent<ColorGroup>();

            EditorApplication.delayCall += () => {
                if (!this) {
                    return;
                }

                LoadTexture();
                Resize(m_AutoSize);
                LoadClipping();
            };
        }

        #endif // UNITY_EDITOR

        #endregion // Editor
    }
}