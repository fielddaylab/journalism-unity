using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;
using FDLocalization;

namespace Journalism.UI {
    public sealed class StorySlotLayout : MonoBehaviour {

        public LocText StoryType;

        [Header("Slots")]
        public StoryBuilderSlot[] Slots;
        public CanvasGroup SlotGroup;
        
        public float SlotWidth;
        public float SlotSpacing;

        public List<StoryBuilderSlot> ActiveSlots = new List<StoryBuilderSlot>(8);
    }
}