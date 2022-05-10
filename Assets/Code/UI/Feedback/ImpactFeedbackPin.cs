using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;

namespace Journalism.UI {
    public sealed class ImpactFeedbackPin : MonoBehaviour {
        public RectTransform Root;
        public Image Icon;
        public SerializedHash32 LocationId;

        [Header("Line")]
        public RectTransform LineAnchor;
        public TextLine Line;
        public TextAlignment Alignment;
    }
}