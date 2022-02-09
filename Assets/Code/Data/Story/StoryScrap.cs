using BeauUtil;
using BeauUtil.Blocks;
using Leaf;
using UnityEngine.Scripting;

namespace Journalism {
    /// <summary>
    /// Scrap of story in player's inventory.
    /// </summary>
    public sealed class StoryScrap : IDataBlock {
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
        Picture,
        Graph,
        Quote,
        Fact,
        Observation
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