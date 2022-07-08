using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using FDLocalization;
using Leaf;
using UnityEngine;
using UnityEngine.Scripting;

namespace Journalism {
    [Serializable]
    public class StoryConfig {
        [LevelLocHint("HeadlineType")] public LocId HeadlineTypeId;
        
        public StorySlot[] Slots;

        [Header("Editor")]
        [LevelLocHint("Brief")] public LocId EditorBriefId;

        [Header("Distribution")]
        [Range(0, 10)] public int FactWeight = 1;
        [Range(0, 10)] public int ColorWeight = 1;
        [Range(0, 10)] public int UsefulWeight = 1;

        [Header("Final")]
        [LevelLocHint("Headline")] public LocId FinalHeadlineId;
    }
}