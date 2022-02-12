using UnityEngine;
using BeauUtil;
using Journalism.UI;
using BeauUtil.Variants;
using Leaf.Runtime;
using BeauRoutine;
using BeauRoutine.Extensions;

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

        private Routine m_FaderRoutine;

        #region Unity Events

        private void Awake() {
            Game.Events.Register<TableKeyPair>(Events.VariableUpdated, OnVariableUpdated, this)
                .Register(Events.SaveDeclared, OnSaveLoaded);

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

        private void OnSaveLoaded() {
            RefreshHeaderEnabled();
        }

        private void RefreshHeaderEnabled() {
            bool showHeader = Player.ReadVariable(Var_HeaderEnabled).AsBool();
            if (showHeader) {
                m_HeaderWindow.Show();
            } else {
                m_HeaderWindow.Hide();
            }
        }

        private void OnHeaderShow(BasePanel.TransitionType _) {
            if (m_HeaderUnderFader.alpha < 1) {
                m_HeaderUnderFader.gameObject.SetActive(true);
                m_FaderRoutine.Replace(this, m_HeaderUnderFader.FadeTo(1, 0.2f));
            }
        }

        private void OnHeaderHide(BasePanel.TransitionType _) {
            if (m_HeaderUnderFader.alpha > 0) {
                m_FaderRoutine.Replace(this, m_HeaderUnderFader.FadeTo(0, 0.2f).OnComplete(() => m_HeaderUnderFader.gameObject.SetActive(false)));
            }
        }

        #endregion // Handlers

        #region Leaf

        [LeafMember("SetHeaderEnabled")]
        static private void SetHeaderEnabled(bool enabled) {
            Player.WriteVariable(Var_HeaderEnabled, enabled);
        }

        #endregion // Leaf
    }
}