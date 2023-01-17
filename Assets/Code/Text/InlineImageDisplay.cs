using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
using Journalism.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism {
    [DisallowMultipleComponent]
    public sealed class InlineImageDisplay : MonoBehaviour {

        public delegate void SelectDelegate(InlineImageDisplay display, bool selected);
        [Serializable] public sealed class Pool : SerializablePool<InlineImageDisplay> { }
        
        #region Inspector

        [Required] public TextLine Line;
        
        [Required] public GameObject TextureGroup;
        [Required] public StreamingUGUITexture Texture;
        public MaskingGroup Masks;
        public GameObject ImageOnly;

        #endregion // Inspector

        [NonSerialized] public Routine Animation;

        public SelectDelegate OnSelectChanged;

        private void Awake() {

        }

        private void OnDisable() {
            Animation.Stop();
        }
    }
}