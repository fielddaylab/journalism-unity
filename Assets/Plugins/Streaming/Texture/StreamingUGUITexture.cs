#if UNITY_2018_3_OR_NEWER
#define USE_ALWAYS
#endif // UNITY_2018_3_OR_NEWER

using System;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace StreamingAssets {
    #if USE_ALWAYS
    [ExecuteAlways]
    #else
    [ExecuteInEditMode]
    #endif // USE_ALWAYS
    [AddComponentMenu("Streaming Assets/Streaming UGUI Texture")]
    [RequireComponent(typeof(RawImage), typeof(RectTransform))]
    public sealed class StreamingUGUITexture : MonoBehaviour, IStreamingComponent {
        #region Inspector

        [SerializeField] private RawImage m_RawImage;

        [SerializeField, StreamingImagePath] private string m_Path;
        [SerializeField] private AutoSizeMode m_AutoSize = AutoSizeMode.Disabled;

        #endregion // Inspector

        [NonSerialized] private Texture2D m_LoadedTexture;
 
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
                return;
            }

            RectTransform rect = (RectTransform) transform;

            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                UnityEditor.Undo.RecordObject(rect, "Changing size");
                UnityEditor.EditorUtility.SetDirty(rect);
            }
            #endif // UNITY_EDITOR

            Vector2 sizeDelta = rect.sizeDelta;

            switch(sizeMode) {
                case AutoSizeMode.StretchX: {
                    float height = m_LoadedTexture.height;
                    if (height > 0) {
                        sizeDelta.x = sizeDelta.y * (m_LoadedTexture.width / height);
                    }
                    break;
                }

                case AutoSizeMode.StretchY: {
                    float width = m_LoadedTexture.width;
                    if (width > 0) {
                        sizeDelta.y = sizeDelta.x * (m_LoadedTexture.height / width);
                    }
                    break;
                }
            }

            rect.sizeDelta = sizeDelta;
        }

        #region Unity Events

        private void OnEnable() {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                m_RawImage = GetComponent<RawImage>();
                LoadTexture();
                return;
            }
            #endif // UNITY_EDITOR

            LoadTexture();
        }

        private void OnDisable() {
            Unload();
        }

        private void OnDestroy() {
            Unload();
        }

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

        #region Editor

        #if UNITY_EDITOR

        private void Reset() {
            m_RawImage = GetComponent<RawImage>();
        }

        private void OnValidate() {
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