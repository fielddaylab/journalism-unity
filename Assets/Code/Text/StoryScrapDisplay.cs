using System;
using BeauPools;
using BeauUtil;
using StreamingAssets;
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

        #endregion // Inspector

        [NonSerialized] public StoryScrapData Data;

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