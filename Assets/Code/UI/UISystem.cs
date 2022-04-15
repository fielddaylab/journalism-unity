using UnityEngine;
using BeauUtil;
using Journalism.UI;
using BeauUtil.Variants;
using Leaf.Runtime;
using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using UnityEngine.Scripting;
using System.Collections;

namespace Journalism.UI {
    public class UISystem : MonoBehaviour {

        #region Consts

        static private readonly TableKeyPair Var_HeaderEnabled = TableKeyPair.Parse("ui:header-enabled");
        static private readonly TableKeyPair Var_ShowStory = TableKeyPair.Parse("ui:show-story");

        #endregion // Consts

        #region Inspector

        [SerializeField] private HeaderUI m_Header = null;
        [SerializeField] private HeaderWindow m_HeaderWindow = null;
        [SerializeField] private CanvasGroup m_HeaderUnderFader = null;
        [SerializeField] private CanvasGroup m_SolidBGFader = null;
        [SerializeField] private GameOverWindow m_GameOver = null;

        #endregion // Inspector

        private Routine m_FaderRoutine;
        private Routine m_SolidBGRoutine;
        [NonSerialized] private bool m_SolidBGState;

        public GameOverWindow GameOver { get { return m_GameOver; } }

        #region Unity Events

        private void Awake() {
            Game.Events.Register<TableKeyPair>(GameEvents.VariableUpdated, OnVariableUpdated, this)
                .Register(GameEvents.LevelLoading, OnLevelLoading, this)
                .Register(GameEvents.LevelStarted, OnLevelStarted, this)
                .Register(GameEvents.GameOver, OnGameOver, this)
                .Register(GameEvents.EditorNotesOpen, OnNeedSolidBG, this)
                .Register(GameEvents.EditorNotesClose, OnNoLongerNeedSolidBG, this)
                .Register(GameEvents.GameOverClose, OnNoLongerNeedSolidBG)
                .Register(GameEvents.RequireStoryPublish, OnRequirePublish, this);

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
            OnNeedSolidBG();
        }

        private void OnLevelLoading() {
            m_HeaderWindow.Hide();
            m_GameOver.Hide();
            OnNoLongerNeedSolidBG();
        }

        private void OnLevelStarted() {
            RefreshHeaderEnabled();
            m_GameOver.InstantHide();
            m_SolidBGFader.Hide();
            m_SolidBGState = false;
            m_SolidBGRoutine.Stop();
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

        private void OnNeedSolidBG() {
            if (m_SolidBGState) {
                return;
            }

            m_SolidBGState = true;
            m_SolidBGRoutine.Replace(this, m_SolidBGFader.Show(0.2f));
        }

        private void OnNoLongerNeedSolidBG() {
            if (!m_SolidBGState) {
                return;
            }

            m_SolidBGState = false;
            m_SolidBGRoutine.Replace(this, m_SolidBGFader.Hide(0.2f));
        }

        private void OnRequirePublish() {
            m_Header.FindButton("Notes").Button.isOn = true;
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

        [LeafMember("SetStoryEnabled"), Preserve]
        static public void SetStoryEnabled(bool enabled) {
            Player.WriteVariable(Var_ShowStory, enabled);
        }

        [LeafMember("ActivateStory"), Preserve]
        static public void ActivateStory() {
            Player.WriteVariable(Var_ShowStory, true);
        }

        [LeafMember("StoryEnabled"), Preserve]
        static public bool GetStoryEnabled() {
            return Player.ReadVariable(Var_ShowStory).AsBool();
        }

        [LeafMember("RunPublish"), Preserve]
        static public IEnumerator RunPublish() {
            ActivateStory();
            Game.Events.Dispatch(GameEvents.RequireStoryPublish);
            yield return Game.Events.Wait(GameEvents.StoryPublished);
            yield return 0.2f;
            yield return Game.Scripting.DisplayNewspaper();
        }

        #endregion // Leaf
    }
}