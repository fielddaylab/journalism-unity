using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using System;
using BeauPools;

namespace Journalism {
    public sealed class TextChoice : MonoBehaviour {
        
        [Serializable] public sealed class Pool : SerializablePool<TextChoice> { }

        #region Inspector

        public TextLine Line;
        public Button Button;

        #endregion // Inspector
    }
}