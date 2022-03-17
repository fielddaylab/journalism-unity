using BeauUtil;
using Leaf;
using UnityEngine;
using System;

namespace Journalism {
    [CreateAssetMenu(menuName = "Journalism Content/Level Definition")]
    public sealed class LevelDef : ScriptableObject {
        
        [Required] public LeafAsset Script;
        
        [Header("Story")]
        public StoryConfig Story;
        [Required] public StoryScraps StoryScraps;
        
        [Header("Additional Assets")]
        public AudioBundle Audio;

        [NonSerialized] public int LevelIndex;
        [NonSerialized] public Script LoadedScript;
    }
}