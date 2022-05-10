using System;

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