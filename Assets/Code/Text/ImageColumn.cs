using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauRoutine;
using BeauPools;
using StreamingAssets;

namespace Journalism {
    public sealed class ImageColumn : MonoBehaviour {
        
        #region Inspector

        public RectTransform Root;

        [Header("Texture")]
        public CanvasGroup TextureGroup;
        public StreamingUGUITexture Texture;

        #endregion // Inspector
    }
}