using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using System;
using BeauPools;
using EasyAssetStreaming;
using BeauUtil.UI;
using FDLocalization;
using Journalism.UI;

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
        [Required] public RectTransform Offset;
        public RectTransform Inner;
        [Required] public CanvasGroup Group;

        [Header("Configuration")]
        [Required] public ColorGroup[] BackgroundColor;
        [Required] public ColorGroup[] OutlineColor;
        [Required] public Image[] Rounding;
        public RawImage[] Pattern;
        public Mask PatternMask;
        public RectTransform Tail;
        [ShowIfField("Tail")] public RectTransform InnerTail;
        [ShowIfField("Text")] public MaxSizeLayoutConstraint MaxSize;

        [Header("Layout")]
        public LayoutGroup Layout;
        [ShowIfField("Tail")] public float TailHeight;

        [Header("Contents")]
        public LocText Text;
        public LocText CharacterHeader;
        public Image HeaderBG;
        public Image Icon; // Radial
        public Image MarkerIcon; // Marker

        [Header("Animation")]
        public AnimatedElement AnimElement = null;
        [NonSerialized] private CanvasSpaceTransformation m_SpaceHelper;

        #endregion // Inspector

        [NonSerialized] public TextStyles.StyleData CurrentStyle;
        [NonSerialized] public TextChars.CharData CurrentChar;
        [NonSerialized] public TextAlignment CurrentAlignment;
    }
}