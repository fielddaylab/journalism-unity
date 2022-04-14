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
    public struct StorySlot {
        public StorySlotType Type;
        public bool Wide;
    }

    public enum StorySlotType {
        Any,
        Picture
    }
}