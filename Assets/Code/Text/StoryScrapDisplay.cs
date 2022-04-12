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
    public sealed class StoryScrapDisplay : MonoBehaviour {

        public delegate void SelectDelegate(StoryScrapDisplay display, bool selected);
        [Serializable] public sealed class Pool : SerializablePool<StoryScrapDisplay> { }
        
        #region Inspector

        [Required] public TextLine Line;
        public Toggle Toggle;
        
        [Required] public GameObject TextureGroup;
        [Required] public StreamingUGUITexture Texture;
        public ScrapAttributeDisplay Attributes;
        public MaskingGroup Masks;

        #endregion // Inspector

        [NonSerialized] public StoryScrapData Data;
        [NonSerialized] public Routine Animation;

        public SelectDelegate OnSelectChanged;

        private void Awake() {
            if (Toggle) {
                Toggle.onValueChanged.AddListener((b) => {
                    if (isActiveAndEnabled) {
                        OnSelectChanged?.Invoke(this, b);
                    }
                });
            }
        }

        private void OnDisable() {
            if (Toggle) {
                Toggle.SetIsOnWithoutNotify(false);
            }
            Animation.Stop();
            Data = null;
        }
    }
}