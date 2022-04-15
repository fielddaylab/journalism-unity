using System;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using EasyAssetStreaming;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism {
    [DisallowMultipleComponent]
    public sealed class StoryScoreDisplay : MonoBehaviour {

        #region Inspector

        public TextLine Line;
        public TMP_Text ScoreName;

        #endregion // Inspector
    }
}