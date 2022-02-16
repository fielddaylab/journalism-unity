using BeauUtil;
using BeauUtil.Blocks;
using Leaf;
using UnityEngine.Scripting;

namespace Journalism {
    /// <summary>
    /// Scrap of story in player's inventory.
    /// </summary>
    public sealed class StoryScrapData : IDataBlock {
        public StringHash32 Id;
        [BlockMeta("type")] public StoryScrapType Type;
        [BlockMeta("quality")] public StoryScrapQuality Quality;
        [BlockMeta("image")] public string ImagePath;
        [BlockContent] public string Content;
    }

    /// <summary>
    /// Type of story scrap.
    /// </summary>
    public enum StoryScrapType {
        Picture = 0x01,
        Graph = 0x02,
        Quote = 0x04,
        Fact = 0x08,
        Observation = 0x10
    }

    /// <summary>
    /// Quality of story scrap
    /// </summary>
    public enum StoryScrapQuality {
        Bad,
        Good,
        Great
    }
}