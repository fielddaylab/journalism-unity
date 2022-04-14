using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism {
    [DisallowMultipleComponent]
    public sealed class StoryQualityDisplay : MonoBehaviour {

        #region Inspector

        public TextLine Line;
        public ScrapQualityDisplay[] Icons;

        #endregion // Inspector
    }
}