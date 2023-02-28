using BeauRoutine;
using BeauRoutine.Extensions;
using EasyAssetStreaming;
using Journalism.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Journalism {
    public class CreditsWindow : BasePanel
    {
        #region Inspector

        [Space(5)]
        [Header("Credits")]

        [SerializeField] private Transform m_ScrollContainer;
        [SerializeField] private Transform m_FinalPanel;
        [SerializeField] private float m_Delay;
        [SerializeField] private float m_Duration;

        #endregion // Inspector

        private float m_ScrollSpeed;
        private float m_ScrollTimer;
        private Routine m_ScrollRoutine;
        private Vector2 m_StartPos;

        private IEnumerator Scroll() {
            yield return m_Delay;

            while (m_ScrollTimer > 0) {

                m_ScrollContainer.Translate(Vector3.up * m_ScrollSpeed * Time.deltaTime * this.transform.localScale.y);
                m_ScrollTimer -= Time.deltaTime;

                yield return null;
            }

            // End credits
            Hide();
        }


        #region BasePanel

        protected override IEnumerator TransitionToShow() {
            m_RootGroup.alpha = 0;
            m_RootTransform.gameObject.SetActive(true);
            while (Streaming.IsLoading()) {
                yield return null;
            }
            yield return m_RootGroup.Show(0.3f);
        }

        protected override IEnumerator TransitionToHide() {
            yield return m_RootGroup.Hide(0.3f);
        }

        protected override void InstantTransitionToShow() {
            m_RootGroup.Show();
        }

        protected override void InstantTransitionToHide() {
            m_RootGroup.Hide();
        }

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            m_StartPos = m_ScrollContainer.transform.localPosition;
            m_ScrollSpeed = (Mathf.Abs(m_FinalPanel.localPosition.y) + Screen.height * 0.75f) / (m_Duration);
            m_ScrollTimer = m_Duration;

            // being scrolling
            m_ScrollRoutine.Replace(Scroll());
        }

        protected override void OnHideComplete(bool inbInstant) {
            base.OnHide(inbInstant);

            // reset position to start
            m_ScrollContainer.transform.localPosition = m_StartPos;

            Game.Events?.Dispatch(GameEvents.PrepareTitleReturn);
            Game.Events?.Dispatch(GameEvents.LoadTitleScreen);
        }

        #endregion // BasePanel
    }
}