using BeauUtil;
using BeauUtil.Blocks;
using Leaf;
using UnityEngine.Scripting;
using UnityEngine;
using System;

namespace Journalism {
    [CreateAssetMenu(menuName = "Journalism Content/Level Definition")]
    public sealed class LevelDef : ScriptableObject {
        
        [Required] public LeafAsset Script;
        [Required] public StoryScraps StoryScraps;

        [NonSerialized] public Script LoadedScript;
    }
}