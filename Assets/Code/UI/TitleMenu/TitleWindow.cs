using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism.UI
{
    public sealed class TitleWindow : BasePanel
    {
        #region Inspector

        [Header("Title")]
        [SerializeField] private RectTransform m_BG;
        [SerializeField] private float m_TransitionTime;

        [SerializeField] private CanvasGroup m_HubPage;
        [SerializeField] private Button m_HubNewGameButton;
        [SerializeField] private Button m_HubResumeGameButton;

        [Space(10)]
        [SerializeField] private CanvasGroup m_NewPage;
        [SerializeField] private RectTransform m_NewPageTransform;
        [SerializeField] private Button m_NewBackButton;
        [SerializeField] private TMP_Text m_NewNameText;
        [SerializeField] private Button m_NewPlayButton;
        [SerializeField] private Toggle m_NewToggleFullscreen = null;
        [SerializeField] private Slider m_NewVolumeSlider = null;

        [Space(10)]
        [SerializeField] private CanvasGroup m_ContinuePage;
        [SerializeField] private RectTransform m_ContinuePageTransform;
        [SerializeField] private Button m_ContinueBackButton;
        [SerializeField] private TMP_InputField m_ContinueNameText;
        [SerializeField] private Button m_ContinuePlayButton;
        [SerializeField] private TMP_InputField m_ContinueInputField;
        [SerializeField] private Toggle m_ContinueToggleFullscreen = null;
        [SerializeField] private Slider m_ContinueVolumeSlider = null;

        [Space(10)]
        [SerializeField] private CanvasGroup m_ErrorPanel;
        [SerializeField] private TMP_Text m_ErrorText;
        [NonSerialized] private Routine m_ErrorAnim;

        [Space(10)]
        [SerializeField] private AudioSystem m_AudioSystem;
        [SerializeField] private string m_OpeningAudioURL;

        private bool m_LoadedFromPlaythrough;

        static private float START_Y = 200;
        static float TRANSITION_Y = -413;

        #endregion //  Inspector

        #region Unity Callbacks

        protected override void Awake() {
            base.Awake();

            m_LoadedFromPlaythrough = false;

            m_HubNewGameButton.onClick.AddListener(OnHubPlayButtonClicked);
            m_HubResumeGameButton.onClick.AddListener(OnHubContinueButtonClicked);

            m_NewBackButton.onClick.AddListener(OnNewBackButtonClicked);
            m_NewPlayButton.onClick.AddListener(OnNewPlayButtonClicked);

            m_ContinueBackButton.onClick.AddListener(OnContinueBackButtonClicked);
            m_ContinuePlayButton.onClick.AddListener(OnContinuePlayButtonClicked);

            m_HubPage.alpha = 1;

            Game.Events.Register<string>(GameEvents.NewNameGenerated, OnNewNameGenerated, this)
                .Register<string>(GameEvents.ContinueNameRetrieved, OnContinueNameRetrieved, this)
                .Register<string>(GameEvents.TitleErrorReceived, OnTitleErrorReceived, this)
                .Register(GameEvents.PrepareTitleReturn, OnPrepareTitleReturn, this);
        }

        #endregion // Unity Callbacks

        #region Handlers

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            m_HubPage.alpha = 1;
            m_NewPage.alpha = 0;
            m_ContinuePage.alpha = 0;

            m_NewToggleFullscreen.onValueChanged.AddListener(HandleFullscreen);
            m_ContinueToggleFullscreen.onValueChanged.AddListener(HandleFullscreen);

            m_NewVolumeSlider.SetValueWithoutNotify(m_AudioSystem.GetMaxVolume());
            m_ContinueVolumeSlider.SetValueWithoutNotify(m_AudioSystem.GetMaxVolume());

            m_NewVolumeSlider.onValueChanged.AddListener(HandleVolumeAdjusted);
            m_ContinueVolumeSlider.onValueChanged.AddListener(HandleVolumeAdjusted);

            StartAudio();

            // if player returns to title menu from game, start at continue screen
            if (m_LoadedFromPlaythrough) {
                OnHubContinueButtonClicked();
            }
        }

        private void OnPrepareTitleReturn() {
            m_LoadedFromPlaythrough = true;
        }

        private void OnHubPlayButtonClicked() {
            Routine.Start(HubNewGameTransition());

            m_HubNewGameButton.interactable = false;
            m_NewToggleFullscreen.isOn = Screen.fullScreen;

        }

        private IEnumerator HubNewGameTransition() {
            m_NewPlayButton.interactable = false;

            yield return Routine.Combine(
                m_NewPage.FadeTo(1, 0.5f),
                m_HubPage.FadeTo(0, 0.5f),
                m_BG.AnchorPosTo(TRANSITION_Y, m_TransitionTime, Axis.Y).Ease(Curve.CubeOut),
                m_NewPageTransform.AnchorPosTo(-91, m_TransitionTime, Axis.Y).Ease(Curve.CubeOut)
                );

            Game.Events.Dispatch(GameEvents.TryNewName);
        }

        private void OnHubContinueButtonClicked() {
            Routine.Start(HubContinueGameTransition());

            m_HubResumeGameButton.interactable = false;
            m_ContinueToggleFullscreen.isOn = Screen.fullScreen;
        }

        private IEnumerator HubContinueGameTransition() {
            yield return Routine.Combine(
                m_ContinuePage.FadeTo(1, 0.5f),
                m_HubPage.FadeTo(0, 0.5f),
                m_BG.AnchorPosTo(TRANSITION_Y, m_TransitionTime, Axis.Y).Ease(Curve.CubeOut),
                m_ContinuePageTransform.AnchorPosTo(-91, m_TransitionTime, Axis.Y).Ease(Curve.CubeOut)
                );

            Game.Events.Dispatch(GameEvents.TryContinueName);
        }

        private void OnNewBackButtonClicked() {
            Routine.Start(NewBackButtonTransition());

            m_HubNewGameButton.interactable = true;
            m_NewPlayButton.interactable = false;
        }

        private IEnumerator NewBackButtonTransition() {
            yield return Routine.Combine(
                m_HubPage.FadeTo(1, 0.5f),
                m_NewPage.FadeTo(0, 0.5f),
                m_BG.AnchorPosTo(START_Y, m_TransitionTime, Axis.Y).Ease(Curve.CubeOut),
                m_NewPageTransform.AnchorPosTo(564, m_TransitionTime, Axis.Y).Ease(Curve.CubeOut)
                );
        }

        private void OnNewPlayButtonClicked() {
            Game.Events.Dispatch(GameEvents.TryNewGame, m_NewNameText.text);
        }

        private void OnContinueBackButtonClicked() {
            Routine.Start(ContinueBackButtonTransition());

            m_HubResumeGameButton.interactable = true;
            m_ContinuePlayButton.interactable = false;
        }

        private IEnumerator ContinueBackButtonTransition() {
            yield return Routine.Combine(
                m_HubPage.FadeTo(1, 0.5f),
                m_ContinuePage.FadeTo(0, 0.5f),
                m_BG.AnchorPosTo(START_Y, m_TransitionTime, Axis.Y).Ease(Curve.CubeOut),
                m_ContinuePageTransform.AnchorPosTo(564, m_TransitionTime, Axis.Y).Ease(Curve.CubeOut)
                );
        }

        private void OnContinuePlayButtonClicked() {
            Game.Events.Dispatch(GameEvents.TryContinueGame, m_ContinueInputField.text);
        }

        private void OnNewNameGenerated(string inName) {
            m_NewNameText.text = inName;

            m_NewPlayButton.interactable = true;

            // Resume Input
            Game.UI.PushInputMask(InputLayerFlags.OverStory);
        }

        private void OnContinueNameRetrieved(string inName) {
            if (!string.IsNullOrEmpty(inName)) {
                m_ContinueNameText.text = inName;
            }

            m_ContinuePlayButton.interactable = true;

            // Resume IInput
            Game.UI.PushInputMask(InputLayerFlags.OverStory);
        }

        private void OnTitleErrorReceived(string errorMsg) {
            m_ErrorAnim.Replace(this, AnimateShowError(errorMsg)).TryManuallyUpdate(0);
        }

        private void HandleFullscreen(bool value) {
            Screen.fullScreen = value;
        }

        private void HandleVolumeAdjusted(float value) {
            m_AudioSystem.SetMaxVolume(value);
        }

        #endregion // Handlers

        public void StartAudio() {
            Game.Audio.SetMusic(m_OpeningAudioURL, 1);
            Game.Audio.SetAmbience(null, 1);
            Game.Audio.SetRain(null, 1);
        }

        #region Animation

        private IEnumerator AnimateShowError(string errorMsg) {
            m_ErrorPanel.alpha = 0;
            m_ErrorText.text = errorMsg;
            m_ErrorPanel.gameObject.SetActive(true);
            yield return m_ErrorPanel.FadeTo(1f, 0.5f);

            yield return 8f;

            yield return m_ErrorPanel.FadeTo(0f, 0.5f);
            m_ErrorPanel.gameObject.SetActive(false);
        }

        #endregion // Animation
    }
}