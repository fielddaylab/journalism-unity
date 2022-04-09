using UnityEngine;
using BeauUtil;
using Journalism.UI;
using BeauUtil.Variants;
using Leaf.Runtime;
using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using UnityEngine.Scripting;

namespace Journalism.UI {
    public class UISystem : MonoBehaviour {

        #region Consts

        static private readonly TableKeyPair Var_HeaderEnabled = TableKeyPair.Parse("ui:header-enabled");

        #endregion // Consts

        #region Inspector

        [SerializeField] private HeaderUI m_Header = null;
        [SerializeField] private HeaderWindow m_HeaderWindow = null;
        [SerializeField] private CanvasGroup m_HeaderUnderFader = null;
        [SerializeField] private GameOverWindow m_GameOver = null;

        #endregion // Inspector

        private Routine m_FaderRoutine;

        public GameOverWindow GameOver { get { return m_GameOver; } }

        #region Unity Events

        private void Awake() {
            Game.Events.Register<TableKeyPair>(GameEvents.VariableUpdated, OnVariableUpdated, this)
                .Register(GameEvents.LevelLoading, OnLevelLoading, this)
                .Register(GameEvents.LevelStarted, OnLevelStarted, this)
                .Register(GameEvents.GameOver, OnGameOver, this);

            m_HeaderUnderFader.gameObject.SetActive(false);
            m_HeaderUnderFader.alpha = 0;

            m_HeaderWindow.OnShowEvent.AddListener(OnHeaderShow);
            m_HeaderWindow.OnHideEvent.AddListener(OnHeaderHide);
        }

        private void OnDestroy() {
            Game.Events?.DeregisterAll(this);
        }

        #endregion // Unity Events

        #region Handlers

        private void OnVariableUpdated(TableKeyPair varId) {
            if (varId == Var_HeaderEnabled) {
                RefreshHeaderEnabled();
            }
        }

        private void OnGameOver() {
            m_HeaderWindow.Hide();
        }

        private void OnLevelLoading() {
            m_HeaderWindow.Hide();
            m_GameOver.Hide();
        }

        private void OnLevelStarted() {
            RefreshHeaderEnabled();
            m_GameOver.InstantHide();
        }

        private void RefreshHeaderEnabled() {
            bool showHeader = Player.ReadVariable(Var_HeaderEnabled).AsBool();
            if (showHeader) {
                m_HeaderWindow.Show();
            } else {
                m_HeaderWindow.Hide();
            }
        }

        private void OnHeaderShow(BasePanel.TransitionType type) {
            if (type == BasePanel.TransitionType.Instant) {
                m_HeaderUnderFader.gameObject.SetActive(true);
                m_HeaderUnderFader.alpha = 1;
            } else if (m_HeaderUnderFader.alpha < 1) {
                m_HeaderUnderFader.gameObject.SetActive(true);
                m_FaderRoutine.Replace(this, m_HeaderUnderFader.FadeTo(1, 0.2f));
            }
        }

        private void OnHeaderHide(BasePanel.TransitionType type) {
            if (type == BasePanel.TransitionType.Instant) {
                m_HeaderUnderFader.gameObject.SetActive(false);
                m_HeaderUnderFader.alpha = 0;
            } else if (m_HeaderUnderFader.alpha > 0) {
                m_FaderRoutine.Replace(this, m_HeaderUnderFader.FadeTo(0, 0.2f).OnComplete(() => m_HeaderUnderFader.gameObject.SetActive(false)));
            }
        }

        #endregion // Handlers

        #region Leaf

        [LeafMember("SetHeaderEnabled"), Preserve]
        static public void SetHeaderEnabled(bool enabled) {
            Player.WriteVariable(Var_HeaderEnabled, enabled);
        }

        [LeafMember("HeaderEnabled"), Preserve]
        static public bool GetHeaderEnabled() {
            return Player.ReadVariable(Var_HeaderEnabled).AsBool();
        }

        [LeafMember("OpenWindow"), Preserve]
        static public void OpenWindow(StringHash32 id) {
            Game.UI.m_Header.FindButton(id).Button.isOn = true;
        }

        #endregion // Leaf
    }
}