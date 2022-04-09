using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauRoutine;
using BeauPools;

namespace Journalism {
    public sealed class TextChoiceGroup : MonoBehaviour {
        
        public struct ChoiceState {
            public TempAlloc<TextChoice> Choice;
            public float LocationX;
            public float LocationY;
            public Routine LocationAnimation;
            public Routine RevealAnimation;
            public Routine StyleAnimation;
        }

        #region Inspector

        [Header("Default Choice")]
        public CanvasGroup DefaultChoiceGroup = null;
        public TextChoice DefaultNextButton = null;
        public TextChoice DefaultBackButton = null;
        public Sprite DefaultNextIcon = null;
        public Color DefaultNextIconColor = Color.black;

        [Header("Grid")]
        public RectTransform GridRoot;
        public CanvasGroup GridGroup;
        public float RowSpacing = 128;
        public float ColumnSpacing = 320;
        public float RotationRange = 1;
        public int MaxOptions = 6;

        [Header("Animation")]
        public TweenSettings NewChoiceAnimParams = new TweenSettings(0.3f, Curve.BackOut);
        public TweenSettings VanishAnimParams = new TweenSettings(0.4f, Curve.QuadIn);
        [Range(0, 1)] public float NewScrollDelay = 0.05f;
        [Range(0, 1)] public float TextVanishDelay = 0.08f;
        public float NewAnimDistance = 80;
        public float VanishAnimDistance = 80;

        #endregion // Inspector

        public RingBuffer<ChoiceState> Choices = new RingBuffer<ChoiceState>();
    }
}