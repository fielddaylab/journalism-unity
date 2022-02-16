using UnityEngine;
using TMPro;
using BeauRoutine;
using UnityEngine.UI;
using BeauUtil;
using System;

namespace Journalism.UI {
    public sealed class HeaderUI : MonoBehaviour {

        #region Inspector

        public CanvasGroup Fader = null;
        public HeaderButton[] Buttons = null;
        public ToggleGroup ToggleGroup = null;

        #endregion // Inspector

        private Routine m_FaderRoutine;
        [NonSerialized] private bool m_FaderEvaluateQueued;
        [NonSerialized] private bool m_FaderState;

        private void Awake() {
            Fader.alpha = 0;
            Fader.gameObject.SetActive(false);

            for(int i = 0; i < Buttons.Length; i++) {
                HeaderButton button = Buttons[i];
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

            bool hasSelection = ToggleGroup.ActiveToggle() != null;
            if (hasSelection == m_FaderState) {
                return;
            }

            m_FaderState = hasSelection;
            if (m_FaderState) {
                Fader.gameObject.SetActive(true);
                m_FaderRoutine.Replace(this, Fader.FadeTo(1, 0.2f));
            } else {
                m_FaderRoutine.Replace(this, Fader.FadeTo(0, 0.2f).OnComplete(() => {
                    Fader.gameObject.SetActive(false);
                }));
            }
        }
    }
}