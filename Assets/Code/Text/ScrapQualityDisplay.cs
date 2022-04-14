using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism {
    [DisallowMultipleComponent]
    public sealed class ScrapQualityDisplay : MonoBehaviour {

        #region Inspector

        public RectTransform Great;
        public RectTransform Bad;

        #endregion // Inspector
    }
}