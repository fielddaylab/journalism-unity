using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Journalism.UI
{
    public sealed class TitleWindow : BasePanel
    {
        #region Inspector

        [Header("Title")]
        [SerializeField] private GameObject m_HubPage;
        [SerializeField] private Button m_HubPlayButton;
        [SerializeField] private Button m_HubContinueButton;
        [SerializeField] private Button m_HubOptionsButton;

        [Space(10)]
        [SerializeField] private GameObject m_NewPage;
        [SerializeField] private Button m_NewBackButton;
        [SerializeField] private TMP_Text m_NewNameText;
        [SerializeField] private Button m_NewPlayButton;

        [Space(10)]
        [SerializeField] private GameObject m_ContinuePage;
        [SerializeField] private Button m_ContinueBackButton;
        [SerializeField] private TMP_InputField m_ContinueNameText;
        [SerializeField] private Button m_ContinuePlayButton;
        [SerializeField] private TMP_InputField m_ContinueInputField;

        [Space(10)]
        [SerializeField] private CanvasGroup m_ErrorPanel;
        [SerializeField] private TMP_Text m_ErrorText;
        [NonSerialized] private Routine m_ErrorAnim;

        private bool m_LoadedFromPlaythrough;

        #endregion //  Inspector

        #region Unity Callbacks

        protected override void Awake() {
            base.Awake();

            m_LoadedFromPlaythrough = false;

            m_HubPlayButton.onClick.AddListener(OnHubPlayButtonClicked);
            m_HubContinueButton.onClick.AddListener(OnHubContinueButtonClicked);
            m_HubOptionsButton.onClick.AddListener(OnHubOptionsButtonClicked);

            m_NewBackButton.onClick.AddListener(OnNewBackButtonClicked);
            m_NewPlayButton.onClick.AddListener(OnNewPlayButtonClicked);

            m_ContinueBackButton.onClick.AddListener(OnContinueBackButtonClicked);
            m_ContinuePlayButton.onClick.AddListener(OnContinuePlayButtonClicked);

            m_HubPage.SetActive(true);

            Game.Events.Register<string>(GameEvents.NewNameGenerated, OnNewNameGenerated, this)
                .Register<string>(GameEvents.ContinueNameRetrieved, OnContinueNameRetrieved, this)
                .Register<string>(GameEvents.TitleErrorReceived, OnTitleErrorReceived, this)
                .Register(GameEvents.PrepareTitleReturn, OnPrepareTitleReturn, this);
        }

        #endregion // Unity Callbacks

        #region Handlers

        protected override void OnShow(bool inbInstant) {
            base.OnShow(inbInstant);

            m_HubPage.SetActive(true);
            m_NewPage.SetActive(false);
            m_ContinuePage.SetActive(false);

            // if player returns to title menu from game, start at continue screen
            if (m_LoadedFromPlaythrough) {
                OnHubContinueButtonClicked();
            }
        }

        private void OnPrepareTitleReturn() {
            m_LoadedFromPlaythrough = true;
        }

        private void OnHubPlayButtonClicked() {
            m_NewPlayButton.interactable = false;
            m_NewPage.SetActive(true);

            m_HubPage.SetActive(false);

            Game.Events.Dispatch(GameEvents.TryNewName);
        }

        private void OnHubContinueButtonClicked() {
            m_ContinuePage.SetActive(true);

            m_HubPage.SetActive(false);

            Game.Events.Dispatch(GameEvents.TryContinueName);
        }

        private void OnHubOptionsButtonClicked() {
            // not implemented
        }

        private void OnNewBackButtonClicked() {
            m_HubPage.SetActive(true);

            m_NewPage.SetActive(false);
        }

        private void OnNewPlayButtonClicked() {
            Game.Events.Dispatch(GameEvents.TryNewGame, m_NewNameText.text);
        }

        private void OnContinueBackButtonClicked() {
            m_HubPage.SetActive(true);

            m_ContinuePage.SetActive(false);
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
            if (!inName.Equals(string.Empty)) {
                m_ContinueNameText.text = inName;
            }

            m_ContinuePlayButton.interactable = true;

            // Resume IInput
            Game.UI.PushInputMask(InputLayerFlags.OverStory);
        }

        private void OnTitleErrorReceived(string errorMsg) {
            m_ErrorAnim.Replace(this, AnimateShowError(errorMsg)).TryManuallyUpdate(0);
        }

        #endregion // Handlers

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