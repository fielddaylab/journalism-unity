using BeauUtil;
using StreamingAssets;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism {
    [DisallowMultipleComponent]
    public sealed class StoryScrapDisplay : MonoBehaviour {
        #region Inspector

        [Required] public TextLine Line;
        
        public ContentSizeFitter ContentSizeFitter;
        public StreamingUGUITexture Texture;

        #endregion // Inspector
    }
}