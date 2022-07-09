﻿using BeauRoutine.Extensions;
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
        

        #endregion //  Inspector

        #region Unity Callbacks

        protected override void Awake() {
            base.Awake();

            m_HubPlayButton.onClick.AddListener(OnHubPlayButtonClicked);
            m_HubContinueButton.onClick.AddListener(OnHubContinueButtonClicked);
            m_HubOptionsButton.onClick.AddListener(OnHubOptionsButtonClicked);

            m_NewBackButton.onClick.AddListener(OnNewBackButtonClicked);
            m_NewPlayButton.onClick.AddListener(OnNewPlayButtonClicked);

            m_ContinueBackButton.onClick.AddListener(OnContinueBackButtonClicked);
            m_ContinuePlayButton.onClick.AddListener(OnContinuePlayButtonClicked);

            m_HubPage.SetActive(true);

            Game.Events.Register<string>(GameEvents.NewNameGenerated, OnNewNameGenerated, this)
                .Register<string>(GameEvents.ContinueNameRetrieved, OnContinueNameRetrieved, this);
        }

        #endregion // Unity Callbacks

        #region Handlers

        private void OnHubPlayButtonClicked() {
            m_NewPlayButton.interactable = false;
            m_NewPage.SetActive(true);

            m_HubPage.SetActive(false);

            Game.Events.Dispatch(GameEvents.TryNewName);
        }

        private void OnHubContinueButtonClicked() {
            m_ContinuePlayButton.interactable = false;
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
        }

        private void OnContinueNameRetrieved(string inName) {
            m_ContinueNameText.text = inName;

            m_ContinuePlayButton.interactable = true;
        }

        #endregion // Handlers
    }
}