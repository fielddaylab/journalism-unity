using System;
using System.Collections;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using Leaf.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism.UI {
    public class TutorialArrow : MonoBehaviour {
        #region Inspector

        [SerializeField] private Canvas m_Canvas = null;
        [SerializeField] private AnimatedElement m_Element = null;

        #endregion // Inspector

        [NonSerialized] private CanvasSpaceTransformation m_SpaceHelper;
        private Routine m_Anim;
        [NonSerialized] private bool m_Activated;

        private void Awake() {
            m_SpaceHelper.CanvasCamera = m_Canvas.worldCamera;
            m_SpaceHelper.CanvasSpace = m_Element.RectTransform;

            AnimatedElement.Hide(m_Element);
        }

        public void Focus(Transform inTransform, Vector2 offset) {
            bool bSnap = !AnimatedElement.IsActive(m_Element);

            Vector2 localPos = GetLocation(inTransform);
            localPos += offset;
            float rotation = 0;
            if (offset != default) {
                rotation = Mathf.Atan2(-offset.y, -offset.x) * Mathf.Rad2Deg;
            }
            if (bSnap) {
                m_Element.RectTransform.localPosition = localPos;
                m_Element.RectTransform.SetRotation(rotation, Axis.Z, Space.Self);
            }

            m_Anim.Replace(this, Activate(localPos, rotation)).ExecuteWhileDisabled();
            m_Activated = true;
        }

        public void Hide(bool instant = false) {
            if (!m_Activated)
                return;

            m_Activated = false;
            if (instant) {
                m_Anim.Stop();
                AnimatedElement.Hide(m_Element, false);
            } else {
                m_Anim.Replace(this, Deactivate()).ExecuteWhileDisabled();
            }

        }

        private IEnumerator Activate(Vector2 inPos, float rotation) {
            return Routine.Combine(
                m_Element.RectTransform.MoveTo(inPos, 0.3f, Axis.XY, Space.Self).Ease(Curve.CubeOut),
                m_Element.RectTransform.RotateTo(rotation, 0.3f, Axis.Z, Space.Self).Ease(Curve.CubeOut),
                AnimatedElement.Show(m_Element, 0.3f, false)
            );
        }

        private IEnumerator Deactivate() {
            return AnimatedElement.Hide(m_Element, 0.3f);
        }

        private Vector2 GetLocation(Transform inFocusTransform) {
            Vector3 pos;
            inFocusTransform.TryGetCamera(out m_SpaceHelper.WorldCamera);
            m_SpaceHelper.TryConvertToLocalSpace(inFocusTransform, out pos);
            return (Vector2)pos;
        }
    }
}