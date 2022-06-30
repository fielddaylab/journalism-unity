using BeauUtil;
using BeauUtil.Blocks;
using Leaf;
using UnityEngine.Scripting;
using UnityEngine;
using System;

namespace Journalism {
    /// <summary>
    /// Scrap of story in player's inventory.
    /// </summary>
    public sealed class StoryScrapData : IDataBlock {
        public StringHash32 Id;
        [BlockMeta("type")] public StoryScrapType Type;
        [BlockMeta("quality")] public StoryScrapQuality Quality = StoryScrapQuality.Good;
        [BlockMeta("image")] public string ImagePath;
        [BlockMeta("align")] public TextAnchor Alignment = TextAnchor.MiddleCenter;
        [BlockMeta("attr")] public StoryScrapAttribute Attributes = 0;
        [BlockContent] public string Content;

        static public bool ShouldContainImage(StoryScrapType type) {
            switch(type) {
                case StoryScrapType.Picture:
                case StoryScrapType.Graph:
                    return true;

                default:
                    return false;
            }
        }

        //hopefully this isn't redundant with ShouldContainImage
        public bool IsPicture(){
            if (this.Type == StoryScrapType.Picture || this.Type == StoryScrapType.Graph || this.Type == StoryScrapType.Photo){
                return true;
            } else return false;
        }
    }

    /// <summary>
    /// Type of story scrap.
    /// </summary>
    [Flags]
    public enum StoryScrapType {
        Picture = 0x01,
        Photo = Picture,
        
        Graph = 0x02,
        Quote = 0x04,
        Fact = 0x08,
        Observation = 0x10,

        [Hidden]
        ImageMask = Picture | Graph,

        [Hidden]
        AnyMask = Picture | Graph | Quote | Fact | Observation
    }

    /// <summary>
    /// Quality of story scrap
    /// </summary>
    public enum StoryScrapQuality {
        Bad,
        Good,
        Great
    }

    /// <summary>
    /// Attributes applied to story scrap.
    /// </summary>
    [Flags]
    public enum StoryScrapAttribute {
        Facts = 0x01,
        Color = 0x02,
        Useful = 0x04
    }
}