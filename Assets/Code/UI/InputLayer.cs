using UnityEngine;
using BeauUtil;
using System;
using UnityEngine.EventSystems;

namespace Journalism.UI {
    public sealed class InputLayer : MonoBehaviour {
        public BaseRaycaster Raycaster;
        
        [AutoEnum] public InputLayerFlags Type;

        #if UNITY_EDITOR

        private void Reset() {
            Raycaster = GetComponent<BaseRaycaster>();
        }

        #endif // UNITY_EDITOR
    }

    [Flags]
    public enum InputLayerFlags {
        Story = 0x01,
        Toolbar = 0x02,
        OverStory = 0x08,
        GameOver = 0x10,

        [Hidden] AllStory = Story | OverStory,
        [Hidden] All = AllStory | Toolbar | GameOver
    }
}