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
        [AutoEnum] public StoryScrapType AllowedTypes;
        public bool Wide;
    }
}