using BeauUtil;
using Leaf;
using UnityEngine;
using System;

namespace Journalism {
    [CreateAssetMenu(menuName = "Journalism Content/Level Definition")]
    public sealed class LevelDef : ScriptableObject {
        
        [Required] public LeafAsset Script;
        [Required] public StoryScraps StoryScraps;
        public AudioBundle Audio;

        [NonSerialized] public Script LoadedScript;
    }
}