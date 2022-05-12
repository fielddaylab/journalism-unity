using UnityEngine;
using BeauUtil;
using System;
using UnityEngine.EventSystems;

namespace Journalism.UI {
    public sealed class InputElement : MonoBehaviour {
        public SerializedHash32 Id;
        
        private void OnEnable() {
            Game.UI.RegisterInputElement(this);
        }

        private void OnDisable() {
            Game.UI?.DeregisterInputElement(this);
        }
    }
}