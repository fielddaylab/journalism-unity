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

        #endregion // Inspector

        [NonSerialized] private HeaderButton m_TimeButton;
        [NonSerialized] private uint m_LastKnownTime;
        private Routine m_FaderRoutine;

        #region Unity Events

        private void Awake() {
            Game.Events.Register<TableKeyPair>(GameEvents.VariableUpdated, OnVariableUpdated, this)
                .Register(GameEvents.LevelStarted, OnLevelStarted, this)
                .Register<uint>(GameEvents.TimeUpdated, OnTimeUpdated, this);

            m_HeaderUnderFader.gameObject.SetActive(false);
            m_HeaderUnderFader.alpha = 0;

            m_HeaderWindow.OnShowEvent.AddListener(OnHeaderShow);
            m_HeaderWindow.OnHideEvent.AddListener(OnHeaderHide);

            m_TimeButton = FindButton("Time");
        }

        private void OnDestroy() {
            Game.Events?.DeregisterAll(this);
        }

        #endregion // Unity Events

        #region Queries

        private HeaderButton FindButton(StringHash32 id) {
            foreach(var button in m_Header.Buttons) {
                if (button.Id == id) {
                    return button;
                }
            }

            return null;
        }

        #endregion // Queries

        #region Handlers

        private void OnTimeUpdated(uint time) {
            if (m_LastKnownTime == time) {
                return;
            }

            int change = (int) (time - m_LastKnownTime);
            m_LastKnownTime = time;
            m_TimeButton.Button.interactable = time > 0;

            // TODO: Animation/SFX for time increase/decrease
        }

        private void OnVariableUpdated(TableKeyPair varId) {
            if (varId == Var_HeaderEnabled) {
                RefreshHeaderEnabled();
            }
        }

        private void OnLevelStarted() {
            RefreshHeaderEnabled();
            m_LastKnownTime = Player.Data.TimeRemaining;
            m_TimeButton.Button.interactable = Player.TimeRemaining() > 0;
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
            Game.UI.FindButton(id).Button.isOn = true;
        }

        #endregion // Leaf
    }
}