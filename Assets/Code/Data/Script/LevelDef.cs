using BeauUtil;
using Leaf;
using UnityEngine;
using System;
using FDLocalization;
using System.Reflection;

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

    public class LevelLocHint : LocIdHintAttribute {
        public string ElementName;

        public LevelLocHint(string elementName) {
            ElementName = elementName;
        }

        public override string GetHint(FieldInfo field, object parent, UnityEngine.Object owner) {
            StringSlice name = owner.name;
            if (name.StartsWith("Level")) {
                name = name.Substring(5);
            }
            string fullKey = "Level." + name.ToString() + "." + ElementName;
            return fullKey;
        }
    }
}