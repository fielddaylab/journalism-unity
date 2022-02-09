using UnityEngine;
using TMPro;
using BeauRoutine.Extensions;
using System;
using UnityEngine.UI;
using System.Collections;
using BeauRoutine;

namespace Journalism.UI {
    public sealed class HeaderWindow : BasePanel {
        #region Inspector

        [SerializeField] private Button m_CloseButton = null;
        [SerializeField] private float m_OffscreenPos = 600;
        [SerializeField] private TweenSettings m_ShowAnim = new TweenSettings(0.3f, Curve.CubeOut);
        [SerializeField] private TweenSettings m_HideAnim = new TweenSettings(0.3f, Curve.CubeIn);

        #endregion // Inspector

        public Action LoadData;

        #region Unity Events

        protected override void Awake() {
            base.Awake();

            if (m_CloseButton) {
                m_CloseButton.onClick.AddListener(() => Hide());
            }
        }

        protected override void OnEnable() {
            base.OnEnable();

            if (IsShowing()) {
                LoadData?.Invoke();
            }
        }

        #endregion // Unity Events

        #region Transitions

        protected override IEnumerator TransitionToShow() {
            m_RootTransform.gameObject.SetActive(true);
            yield return m_RootTransform.AnchorPosTo(0, m_ShowAnim, Axis.Y);
        }

        protected override IEnumerator TransitionToHide() {
            yield return m_RootTransform.AnchorPosTo(m_OffscreenPos, m_HideAnim, Axis.Y);
            m_RootTransform.gameObject.SetActive(false);
        }

        protected override void InstantTransitionToShow() {
            m_RootTransform.SetAnchorPos(0, Axis.Y);
            m_RootTransform.gameObject.SetActive(true);
        }

        protected override void InstantTransitionToHide() {
            m_RootTransform.SetAnchorPos(m_OffscreenPos, Axis.Y);
            m_RootTransform.gameObject.SetActive(false);
        }

        #endregion // Transitions

        static private bool ShouldShow() {
            return Player.ReadVariable("ui:showHud").AsBool();
        }
    }
}