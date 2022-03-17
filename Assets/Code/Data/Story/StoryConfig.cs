using System;
using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using Leaf;
using UnityEngine.Scripting;

namespace Journalism {
    [Serializable]
    public class StoryConfig {
        public string HeadlineType;
        public StorySlot[] Slots;
    }
}