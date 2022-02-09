using UnityEngine;
using BeauUtil;
using Journalism.UI;
using BeauUtil.Variants;
using Leaf.Runtime;

namespace Journalism.UI {
    public class UISystem : MonoBehaviour {

        #region Consts

        static private readonly TableKeyPair Var_HeaderEnabled = TableKeyPair.Parse("ui:header-enabled");

        #endregion // Consts

        [SerializeField] private HeaderUI m_Header = null;
        [SerializeField] private HeaderWindow m_HeaderWindow = null;

        #region Unity Events

        private void Awake() {
            Game.Events.Register<TableKeyPair>(Events.VariableUpdated, OnVariableUpdated, this)
                .Register(Events.SaveDeclared, OnSaveLoaded);
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

        #endregion // Handlers

        #region Leaf

        [LeafMember("SetHeaderEnabled")]
        static private void SetHeaderEnabled(bool enabled) {
            Player.WriteVariable(Var_HeaderEnabled, enabled);
        }

        #endregion // Leaf
    }
}