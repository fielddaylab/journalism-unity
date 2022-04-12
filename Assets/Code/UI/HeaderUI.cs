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
        public HeaderWindow Root = null;
        public HeaderButton[] Buttons = null;
        public ToggleGroup ToggleGroup = null;
        public ClockIncrements TimeClock = null;
        public TextLine TimeEffect = null;

        #endregion // Inspector

        [NonSerialized] private HeaderButton m_TimeButton;
        private Routine m_FaderRoutine;
        [NonSerialized] private bool m_FaderEvaluateQueued;
        [NonSerialized] private bool m_FaderState;
        [NonSerialized] private Routine m_TimeEffectAnim;

        private void Awake() {
            Fader.alpha = 0;
            Fader.gameObject.SetActive(false);

            m_TimeButton = FindButton("Time");

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
                button.Window.OnHideCompleteEvent.AddListener((_) => {
                    m_TimeEffectAnim.Stop();
                    TimeEffect.gameObject.SetActive(false);
                });
            }

            Game.Events.Register<Player.TimeUpdateArgs>(GameEvents.TimeUpdated, OnTimeUpdated, this)
                .Register(GameEvents.LevelStarted, OnLevelStarted, this);
        }

        public HeaderButton FindButton(StringHash32 id) {
            foreach(var button in Buttons) {
                if (button.Id == id) {
                    return button;
                }
            }

            return null;
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

        private void OnLevelStarted() {
            m_TimeButton.Button.interactable = Player.TimeRemaining() > 0;

            ClockIncrements.Populate(TimeClock, (int) Player.TimeRemaining());
        }

        private void OnTimeUpdated(Player.TimeUpdateArgs args) {
            m_TimeButton.Button.interactable = args.Units > 0;

            ClockIncrements.Populate(TimeClock, (int) args.Units);

            if (Root.IsShowing()) {
                if (args.Delta < 0) {
                    TimeEffect.gameObject.SetActive(true);
                    GameText.PopulateTextLine(TimeEffect, "-" + GameText.FormatTime((uint) -args.Delta, true), null, default, Assets.Style("time-decrease"));
                    GameText.PrepareTextLine(TimeEffect, 2);
                    m_TimeEffectAnim.Replace(this, GameText.AnimateTextLineEffect(TimeEffect, new Vector2(0, 15), 0.3f, 2));
                }
            }
        }
    }
}