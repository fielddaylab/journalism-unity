using UnityEngine;
using TMPro;
using BeauRoutine;
using UnityEngine.UI;
using BeauUtil;
using System;

namespace Journalism.UI {
    public sealed class HeaderUI : MonoBehaviour {

        #region Inspector

        [SerializeField] private CanvasGroup m_Fader = null;
        [SerializeField] private HeaderButton[] m_Buttons = null;
        [SerializeField] private ToggleGroup m_ToggleGroup = null;

        #endregion // Inspector

        private Routine m_FaderRoutine;
        [NonSerialized] private bool m_FaderEvaluateQueued;
        [NonSerialized] private bool m_FaderState;

        private void Awake() {
            m_Fader.alpha = 0;
            m_Fader.gameObject.SetActive(false);

            for(int i = 0; i < m_Buttons.Length; i++) {
                HeaderButton button = m_Buttons[i];
                if (!button.Window) {
                    continue;
                }
                
                button.Button.onValueChanged.AddListener((b) => {
                    if (b) {
                        button.Window.Show();
                    } else {
                        button.Window.Hide();
                    }
                });
                button.Window.OnShowEvent.AddListener((_) => {
                    button.Button.SetIsOnWithoutNotify(true);
                    QueueFaderEvaluate();
                });
                button.Window.OnHideEvent.AddListener((_) => {
                    button.Button.SetIsOnWithoutNotify(false);
                    QueueFaderEvaluate();
                });
            }
        }

        private void QueueFaderEvaluate() {
            if (!m_FaderEvaluateQueued) {
                m_FaderEvaluateQueued = true;
                Async.InvokeAsync(ReevaluateFaderState);
            }
        }

        private void ReevaluateFaderState() {
            m_FaderEvaluateQueued = false;

            bool hasSelection = m_ToggleGroup.ActiveToggle() != null;
            if (hasSelection == m_FaderState) {
                return;
            }

            m_FaderState = hasSelection;
            if (m_FaderState) {
                m_Fader.gameObject.SetActive(true);
                m_FaderRoutine.Replace(this, m_Fader.FadeTo(1, 0.2f));
            } else {
                m_FaderRoutine.Replace(this, m_Fader.FadeTo(0, 0.2f).OnComplete(() => {
                    m_Fader.gameObject.SetActive(false);
                }));
            }
        }
    }
}