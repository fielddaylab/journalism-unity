using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauRoutine;
using BeauPools;

namespace Journalism {
    public sealed class TextLineScroll : MonoBehaviour {
        
        public struct LineState {
            public TempAlloc<TextLine> Line;
            public float Location;
            public Routine LocationAnimation;
            public Routine RevealAnimation;
        }

        #region Inspector

        public Transform Root;

        [Header("List")]
        public Transform ListRoot;
        public float Spacing = 12;
        public int MaxLines = 8;
        public float RotationRange = 1;
        public TextAnchor Anchor = TextAnchor.LowerCenter;

        [Header("Animation")]
        public TweenSettings NewTextAnimParams = new TweenSettings(0.3f, Curve.BackOut);
        public TweenSettings TextScrollAnimParams = new TweenSettings(0.2f, Curve.BackOut);
        public TweenSettings VanishAnimParams = new TweenSettings(0.4f, Curve.QuadIn);
        [Range(0, 1)] public float TextScrollDelay = 0.05f;
        public float VanishAnimDistance = 80;

        #endregion // Inspector

        public RingBuffer<LineState> Lines = new RingBuffer<LineState>();
    }
}