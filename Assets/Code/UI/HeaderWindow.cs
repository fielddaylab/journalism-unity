using UnityEngine;
using TMPro;
using BeauRoutine.Extensions;
using System;
using UnityEngine.UI;
using System.Collections;
using BeauRoutine;
using EasyAssetStreaming;
using BeauUtil;

namespace Journalism.UI {
    public sealed class HeaderWindow : BasePanel {
        #region Inspector

        [SerializeField] private Button m_CloseButton = null;
        [SerializeField] private float m_OffscreenPos = 600;
        [SerializeField] private TweenSettings m_ShowAnim = new TweenSettings(0.3f, Curve.CubeOut);
        [SerializeField] private TweenSettings m_HideAnim = new TweenSettings(0.3f, Curve.CubeIn);
        [SerializeField] private float m_RandomRotationRange = 0;
        [SerializeField] private SerializedHash32 m_OpenAudio = null;
        [SerializeField] private SerializedHash32 m_CloseAudio = null;

        #endregion // Inspector

        public Action LoadData;
        public Func<IEnumerator> LoadDataAsync;
        public Action UnloadData;

        private AsyncHandle m_LoadAsyncHandle;

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
                IEnumerator asyncLoad = LoadDataAsync?.Invoke();
                if (asyncLoad != null) {
                    m_LoadAsyncHandle = Async.Schedule(asyncLoad, AsyncFlags.MainThreadOnly);
                }
            }

            transform.SetRotation(RNG.Instance.SignNonZero() * RNG.Instance.NextFloat(m_RandomRotationRange * 0.5f, m_RandomRotationRange), Axis.Z, Space.Self);
        }

        protected override void OnDisable() {
            if (WasShowing()) {
                UnloadData?.Invoke();
            }
            m_LoadAsyncHandle.Cancel();

            base.OnDisable();
        }

        #endregion // Unity Events

        #region Transitions

        protected override IEnumerator TransitionToShow() {
            m_RootTransform.gameObject.SetActive(true);
            while(Streaming.IsLoading() || m_LoadAsyncHandle.IsRunning()) {
                yield return null;
            }
            Game.Audio.PlayOneShot(m_OpenAudio);
            yield return m_RootTransform.AnchorPosTo(0, m_ShowAnim, Axis.Y);
        }

        protected override IEnumerator TransitionToHide() {
            Game.Audio.PlayOneShot(m_CloseAudio);
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
    }
}