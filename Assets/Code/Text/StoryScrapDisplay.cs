using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
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
        
        public StreamingUGUITexture Texture;
        public ScrapAttributeDisplay Attributes;

        #endregion // Inspector

        [NonSerialized] public StoryScrapData Data;
        [NonSerialized] public Routine Animation;
        [NonSerialized] public float OriginalLayoutOffset;

        public SelectDelegate OnSelectChanged;

        private void Awake() {
            if (Toggle) {
                Toggle.onValueChanged.AddListener((b) => {
                    OnSelectChanged?.Invoke(this, b);
                });
            }
        }
    }
}