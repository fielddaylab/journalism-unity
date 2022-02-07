#if UNITY_2018_3_OR_NEWER
#define USE_ALWAYS
#endif // UNITY_2018_3_OR_NEWER

using System;
using BeauUtil;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using BeauUtil.Debugger;
using UnityEngine.Rendering;
#endif // UNITY_EDITOR

namespace StreamingAssets {

    #if USE_ALWAYS
    [ExecuteAlways]
    #else
    [ExecuteInEditMode]
    #endif // USE_ALWAYS
    [AddComponentMenu("Streaming Assets/Streaming Quad Texture")]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class StreamingQuadTexture : MonoBehaviour, IStreamingComponent {
        
        #region Inspector

        [SerializeField] private MeshFilter m_MeshFilter;
        [SerializeField] private MeshRenderer m_MeshRenderer;

        [SerializeField, StreamingImagePath] private string m_Path;
        [SerializeField, Required] private Material m_Material;
        [SerializeField] private Color32 m_Color = Color.white;
        
        [SerializeField] private Vector2 m_Size = new Vector2(1, 1);
        [SerializeField] private Vector2 m_Pivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private AutoSizeMode m_AutoSize = AutoSizeMode.Disabled;
        
        [SerializeField, SortingLayer] private int m_SortingLayer = 0;
        [SerializeField] private int m_SortingOrder = 0;

        #endregion // Inspector

        [NonSerialized] private Texture2D m_LoadedTexture;
        [NonSerialized] private Mesh m_MeshInstance;
        [NonSerialized] private Shader m_LastKnownShader = null;
        [NonSerialized] private int m_MainTexturePropertyId = 0;
        [NonSerialized] private int m_MainColorPropertyId = 0;
 
        private readonly Streaming.AssetCallback OnAssetUpdated;

        private StreamingQuadTexture() {
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
            get { return m_Color; }
            set {
                if (m_Color != value) {
                    m_Color = value;
                    if (isActiveAndEnabled) {
                        ApplyColor();
                    }
                }
            }
        }

        /// <summary>
        /// Transparency of the renderer.
        /// </summary>
        public float Alpha {
            get { return m_Color.a / 255f; }
            set {
                if (m_Color.a != value) {
                    m_Color.a = (byte) (value * 255);
                    if (isActiveAndEnabled) {
                        ApplyColor();
                    }
                }
            }
        }

        /// <summary>
        /// Sorting layer id for the renderer.
        /// </summary>
        public int SortingLayerId {
            get { return m_SortingLayer; }
            set {
                if (m_SortingLayer != value) {
                    m_SortingLayer = value;
                    if (isActiveAndEnabled) {
                        ApplySorting();
                    }
                }
            }
        }

        /// <summary>
        /// Sorting order for the renderer.
        /// </summary>
        public int SortingOrder {
            get { return m_SortingOrder; }
            set {
                if (m_SortingOrder != value) {
                    m_SortingOrder = value;
                    if (isActiveAndEnabled) {
                        ApplySorting();
                    }
                }
            }
        }
        
        /// <summary>
        /// Base material.
        /// </summary>
        public Material SharedMaterial {
            get { return m_Material; }
            set {
                if (m_Material != value) {
                    m_Material = value;
                    if (isActiveAndEnabled) {
                        LoadMaterial();
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
                return;
            }

            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                UnityEditor.Undo.RecordObject(this, "Changing size");
                UnityEditor.EditorUtility.SetDirty(this);
            }
            #endif // UNITY_EDITOR

            switch(sizeMode) {
                case AutoSizeMode.StretchX: {
                    float height = m_LoadedTexture.height;
                    if (height > 0) {
                        m_Size.x = m_Size.y * (m_LoadedTexture.width / height);
                    }
                    break;
                }

                case AutoSizeMode.StretchY: {
                    float width = m_LoadedTexture.width;
                    if (width > 0) {
                        m_Size.y = m_Size.x * (m_LoadedTexture.height / width);
                    }
                    break;
                }
            }

            if (isActiveAndEnabled) {
                LoadMesh();
            }
        }

        #region Unity Events

        private void OnEnable() {
            #if UNITY_EDITOR
            if (!Application.IsPlaying(this)) {
                if (EditorApplication.isPlayingOrWillChangePlaymode) {
                    return;
                }
                m_MeshFilter = GetComponent<MeshFilter>();
                m_MeshRenderer = GetComponent<MeshRenderer>();
                Refresh();
                return;
            }
            #endif // UNITY_EDITOR

            Refresh();
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
            if (m_MainTexturePropertyId == 0) {
                LoadMaterial();
            }
            if (!m_MeshInstance) {
                LoadMesh();
            }
        }

        private void Refresh() {
            LoadMaterial();
            LoadTexture();
            ApplySorting();
            LoadMesh();
        }

        private void LoadMaterial() {
            if (!m_Material) {
                if (m_LastKnownShader != null) {
                    m_LastKnownShader = null;
                    m_MainTexturePropertyId = 0;
                    m_MainColorPropertyId = 0;
                }
                return;
            }

            m_MeshRenderer.sharedMaterial = m_Material;
            if (m_Material.shader != m_LastKnownShader) {
                m_LastKnownShader = m_Material.shader;
                m_MainTexturePropertyId = FindMainTexturePropertyName(m_LastKnownShader);
                // Assert.True(m_MainColorPropertyId != 0 && m_Material.HasProperty(m_MainTexturePropertyId), "No main texture property found for shader {0}", m_LastKnownShader.name);

                // if we have a ColorGroup we shouldn't interfere with the color settings there
                if (GetComponent<ColorGroup>()) {
                    m_MainColorPropertyId = 0;
                } else {
                    m_MainColorPropertyId = FindMainColorPropertyName(m_Material);
                }
            }

            ApplyTextureAndColor();
        }

        private void ApplyColor() {
            if (m_MainColorPropertyId != 0) {
                var spb = SharedPropertyBlock(m_MeshRenderer);
                spb.SetColor(m_MainColorPropertyId, m_Color);
                m_MeshRenderer.SetPropertyBlock(spb);
                spb.Clear();
            } else {
                LoadMesh();
            }
        }

        private void LoadTexture() {
            if (!Streaming.Texture(m_Path, ref m_LoadedTexture, OnAssetUpdated)) {
                if (!m_LoadedTexture) {
                    m_MeshRenderer.enabled = false;
                }
                return;
            }

            m_MeshRenderer.enabled = m_LoadedTexture;
            
            if (m_MainTexturePropertyId != 0) {
                ApplyTextureAndColor();
            }

            if (Streaming.IsLoaded(m_LoadedTexture))
                Resize(m_AutoSize);
        }

        private void ApplyTextureAndColor() {
            var spb = SharedPropertyBlock(m_MeshRenderer);
            if (m_LoadedTexture) {
                spb.SetTexture(m_MainTexturePropertyId, m_LoadedTexture);
                if (m_MainColorPropertyId != 0) {
                    spb.SetColor(m_MainColorPropertyId, m_Color);
                }
            } else {
                spb.SetTexture(m_MainTexturePropertyId, Texture2D.whiteTexture);
                if (m_MainColorPropertyId != 0) {
                    spb.SetColor(m_MainColorPropertyId, m_Color);
                }
            }
            m_MeshRenderer.SetPropertyBlock(spb);
            spb.Clear();
        }

        private void ApplySorting() {
            m_MeshRenderer.sortingLayerID = m_SortingLayer;
            m_MeshRenderer.sortingOrder = m_SortingOrder;
        }

        private void LoadMesh() {
            Color32 vertColor;
            if (m_MainColorPropertyId == 0) {
                vertColor = m_Color;
            } else {
                vertColor = Color.white;
            }

            m_MeshInstance = MeshGeneration.CreateQuad(m_Size, m_Pivot, vertColor, m_MeshInstance);
            m_MeshInstance.hideFlags = HideFlags.DontSave;
            m_MeshFilter.sharedMesh = m_MeshInstance;
        }

        /// <summary>
        /// Unloads all resources owned by the StreamingWorldTexture.
        /// </summary>
        public void Unload() {
            if (m_MeshRenderer) {
                m_MeshRenderer.enabled = false;
            }

            Streaming.Unload(ref m_LoadedTexture);
            Streaming.DestroyResource(ref m_MeshInstance);
        }

        #endregion // Resources

        #region Editor

        #if UNITY_EDITOR

        private void Reset() {
            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();
            m_Material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        }

        private void OnValidate() {
            if (EditorApplication.isPlaying || PrefabUtility.IsPartOfPrefabAsset(this)) {
                return;
            }

            m_MeshFilter = GetComponent<MeshFilter>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            EditorApplication.delayCall += () => {
                if (!this) {
                    return;
                }

                if (!m_Material) {
                    m_Material = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
                }

                LoadMaterial();
                LoadTexture();
                ApplySorting();
                Resize(m_AutoSize);
                LoadMesh();
            };
        }

        #endif // UNITY_EDITOR

        #endregion // Editor

        #region Utilities

        static private MaterialPropertyBlock s_SharedPropertyBlock;

        static internal MaterialPropertyBlock SharedPropertyBlock(MeshRenderer renderer) {
            MaterialPropertyBlock b = s_SharedPropertyBlock ?? (s_SharedPropertyBlock = new MaterialPropertyBlock());
            renderer.GetPropertyBlock(b);
            return b;
        }

        static internal int FindMainTexturePropertyName(Shader shader) {
            int propCount = shader.GetPropertyCount();
            for(int i = 0; i < propCount; i++) {
                ShaderPropertyFlags propertyFlags = shader.GetPropertyFlags(i);
                if ((propertyFlags & ShaderPropertyFlags.MainTexture) != 0) {
                    return shader.GetPropertyNameId(i); 
                }
            }
            return Shader.PropertyToID("_MainTex");
        }

        static internal int FindMainColorPropertyName(Material material) {
            Shader shader = material.shader;
            int propCount = shader.GetPropertyCount();
            for(int i = 0; i < propCount; i++) {
                ShaderPropertyFlags propertyFlags = shader.GetPropertyFlags(i);
                if ((propertyFlags & UnityEngine.Rendering.ShaderPropertyFlags.MainColor) != 0) {
                    return shader.GetPropertyNameId(i);
                }
            }

            int baseColor = Shader.PropertyToID("_BaseColor");
            int mainColor = Shader.PropertyToID("_MainColor");
            int color = Shader.PropertyToID("_Color");
            
            if (material.HasProperty(baseColor)) {
                return baseColor;
            } else if (material.HasProperty(mainColor)) {
                return mainColor;
            } else if (material.HasProperty(color)) {
                return color;
            } else {
                return 0;
            }
        }

        #endregion // Utilities
    }
}