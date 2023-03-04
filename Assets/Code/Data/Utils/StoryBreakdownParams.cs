using BeauUtil;
using System.Collections.Concurrent;

namespace Journalism
{
    public struct StoryBreakdownParams
    {
        public StoryConfig TargetStoryStats;
        public StoryStats CurrentStoryStats;

        public StoryBreakdownParams(StoryConfig target, StoryStats current) {
            TargetStoryStats = target;
            CurrentStoryStats = current;
        }
    }
}