using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using System;
using BeauPools;

namespace Journalism {
    public sealed class TextLine : MonoBehaviour {
        
        [Serializable] public sealed class Pool : SerializablePool<TextLine> { }

        public enum TailMode {
            Hidden,
            Left,
            Right,
        }

        #region Inspector

        [Header("Animation")]
        public RectTransform Root;
        public CanvasGroup Group;

        [Header("Configuration")]
        public ColorGroup[] BackgroundColor;
        public ColorGroup[] OutlineColor;
        public RectTransform Tail;

        [Header("Layout")]
        public LayoutGroup Layout;
        public float TailHeight;

        [Header("Contents")]
        public TMP_Text Text;
        public Image Icon;

        #endregion // Inspector

        [NonSerialized] public TextStyles.StyleData CurrentStyle;
    }
}