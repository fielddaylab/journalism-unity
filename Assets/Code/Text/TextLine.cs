using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using System;
using BeauPools;
using EasyAssetStreaming;

namespace Journalism {
    
    [DisallowMultipleComponent]
    public sealed class TextLine : MonoBehaviour {
        
        [Serializable] public sealed class Pool : SerializablePool<TextLine> { }

        public enum TailMode {
            Hidden,
            Left,
            Right,
        }

        #region Inspector

        [Header("Animation")]
        [Required] public RectTransform Root;
        [Required] public CanvasGroup Group;

        [Header("Configuration")]
        [Required] public ColorGroup[] BackgroundColor;
        [Required] public ColorGroup[] OutlineColor;
        [Required] public Image[] Rounding;
        public RectTransform Tail;
        [ShowIfField("Tail")] public RectTransform InnerTail;

        [Header("Layout")]
        public LayoutGroup Layout;
        [ShowIfField("Tail")] public float TailHeight;

        [Header("Contents")]
        public TMP_Text Text;
        public Image Icon;

        #endregion // Inspector

        [NonSerialized] public TextStyles.StyleData CurrentStyle;
        [NonSerialized] public TextAlignment CurrentAlignment;
    }
}