using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauRoutine;
using BeauPools;
using EasyAssetStreaming;

namespace Journalism {
    public sealed class ImageColumn : MonoBehaviour {
        
        #region Inspector

        [Header("Texture")]
        public StreamingUGUITexture Texture;

        #endregion // Inspector
    }
}