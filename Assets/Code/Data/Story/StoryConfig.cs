using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using Leaf;
using UnityEngine;
using UnityEngine.Scripting;

namespace Journalism {
    [Serializable]
    public class StoryConfig {
        public string HeadlineType;
        public StorySlot[] Slots;

        [Header("Editor")]
        [Multiline] public string EditorBrief;

        [Header("Distribution")]
        [Range(0, 10)] public int FactWeight = 1;
        [Range(0, 10)] public int ColorWeight = 1;
        [Range(0, 10)] public int UsefulWeight = 1;
    }
}