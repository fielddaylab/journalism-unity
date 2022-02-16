using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauRoutine;
using BeauPools;
using System;

namespace Journalism {
    public sealed class TextLineScroll : MonoBehaviour {
        
        public struct LineState {
            public TempAlloc<TextLine> LineAlloc;
            public TempAlloc<StoryScrapDisplay> ScrapAlloc;
            public TextLine Line;
            public float LocationX;
            public float LocationY;
            public Routine LocationAnimation;
            public Routine RevealAnimation;
        }

        #region Inspector

        public RectTransform Root;

        [Header("List")]
        public RectTransform ListRoot;
        public float Spacing = 12;
        public int MaxLines = 8;
        public float RotationRange = 1;
        public TextAlignment Alignment = TextAlignment.Center;
        public float DialogInset = 16;

        [Header("Animation")]
        public TweenSettings NewTextAnimParams = new TweenSettings(0.3f, Curve.BackOut);
        public TweenSettings TextScrollAnimParams = new TweenSettings(0.2f, Curve.BackOut);
        public TweenSettings VanishAnimParams = new TweenSettings(0.4f, Curve.QuadIn);
        [Range(0, 1)] public float TextScrollDelay = 0.05f;
        [Range(0, 1)] public float TextVanishDelay = 0.08f;
        public float VanishAnimDistance = 80;

        #endregion // Inspector

        [NonSerialized] public float RootBaseline;
        public RingBuffer<LineState> Lines = new RingBuffer<LineState>();
    }
}