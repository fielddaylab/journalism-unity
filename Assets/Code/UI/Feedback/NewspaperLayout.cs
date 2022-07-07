using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;
using FDLocalization;

namespace Journalism.UI {
    public sealed class NewspaperLayout : MonoBehaviour {

        public RectTransform Root;
        public CanvasGroup Group;

        [Header("Text")]
        public LocText Headline;
        public LocText Author;

        [Header("Slots")]
        public StoryScrapDisplay[] Slots;
        
        public float SlotWidth;
        public float SlotSpacing;
    }
}