using System.Collections;
using System.Collections.Generic;
using BeauUtil;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism.UI {

    public sealed class MaskingGroup : MonoBehaviour, IEnumerable<MaskableGraphic> {
        public MaskableGraphic[] Graphics;

        static public void Enable(MaskingGroup group) {
            foreach(var graphic in group) {
                graphic.maskable = true;
            }
        }

        static public void Disable(MaskingGroup group) {
            foreach(var graphic in group) {
                graphic.maskable = false;
            }
        }

        static public void Update(MaskingGroup group, bool state) {
            foreach(var graphic in group) {
                graphic.maskable = state;
            }
        }

        static public int FindAllMaskable(GameObject root, List<MaskableGraphic> graphics) {
            root.GetComponentsInChildren<MaskableGraphic>(true, graphics);
            for(int i = graphics.Count - 1; i >= 0; i--) {
                if (!graphics[i].maskable) {
                    ListUtils.FastRemoveAt(graphics, i);
                }
            }
            return graphics.Count;
        }

        static public MaskableGraphic[] FindAllMaskable(GameObject root) {
            List<MaskableGraphic> maskables = new List<MaskableGraphic>(8);
            FindAllMaskable(root, maskables);
            return maskables.ToArray();
        }

        #if UNITY_EDITOR

        private void Reset() {
            Graphics = FindAllMaskable(gameObject);
        }

        #endif // UNITY_EDITOR

        #region IEnumerable

        public IEnumerator<MaskableGraphic> GetEnumerator() {
            return ((IEnumerable<MaskableGraphic>)Graphics).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return Graphics.GetEnumerator();
        }

        #endregion // IEnumerable
    }
}