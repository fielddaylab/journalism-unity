using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism {
    [DisallowMultipleComponent]
    public sealed class StoryAttributeDisplay : MonoBehaviour {

        #region Inspector

        public TextLine Line;
        public ScrapAttributeDisplay Target;
        public ScrapAttributeDisplay Current;

        #endregion // Inspector
    }
}