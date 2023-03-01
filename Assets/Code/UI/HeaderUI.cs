using UnityEngine;
using TMPro;
using BeauRoutine;
using UnityEngine.UI;
using BeauUtil;
using System;
using System.Collections;
using BeauUtil.Variants;

namespace Journalism.UI {
    public sealed class HeaderUI : MonoBehaviour {

        static public readonly TableKeyPair Var_StatsEnabled = TableKeyPair.Parse("ui:stats.visible");
        static public readonly TableKeyPair Var_NotesEnabled = TableKeyPair.Parse("ui:notes.visible");

        #region Inspector

        public CanvasGroup Fader = null;
        public HeaderWindow Root = null;
        public HeaderButton[] Buttons = null;
        public ToggleGroup ToggleGroup = null;
        public ClockIncrements TimeClock = null;
        public TextLine TimeEffect = null;
        public TextLine StatsEffect = null;
        public Image StatsRays = null;

        #endregion // Inspector

        [NonSerialized] private HeaderButton m_TimeButton;

        private Routine m_FaderRoutine;
        [NonSerialized] private bool m_FaderEvaluateQueued;
        [NonSerialized] private bool m_FaderState;
        [NonSerialized] private Routine m_StatsEffectAnim;
        [NonSerialized] private Routine m_StatsRaysAnim;
        [NonSerialized] private Routine m_TimeEffectAnim;

        private void Awake() {
            Fader.alpha = 0;
            Fader.gameObject.SetActive(false);

            m_TimeButton = FindButton("Time");

            for (int i = 0; i < Buttons.Length; i++) {
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
                    m_StatsEffectAnim.Stop();
                    TimeEffect.gameObject.SetActive(false);
                    StatsEffect.gameObject.SetActive(false);
                    StatsRays.gameObject.SetActive(false);
                });
            }

            Game.Events.Register<Player.TimeUpdateArgs>(GameEvents.TimeUpdated, OnTimeUpdated, this)
                .Register<int[]>(GameEvents.StatsUpdated, OnStatsUpdated, this)
                .Register(GameEvents.LevelStarted, OnLevelStarted, this)
                .Register<TableKeyPair>(GameEvents.VariableUpdated, OnVariableChanged, this);
        }

        public bool AnyOpen() {
            return m_FaderState || m_FaderRoutine;
        }

        public IEnumerator ShowStatsRays() {
            yield return m_StatsRaysAnim.Replace(this, GameText.AnimateStatsRays(StatsRays, Vector2.zero, 0.3f)).DelayBy(0.55f);
        }

        public HeaderButton FindButton(StringHash32 id) {
            foreach(var button in Buttons) {
                if (button.Id == id) {
                    return button;
                }
            }

            return null;
        }

        private void ReevaluateVisibleButtons() {
            FindButton("Stats").Button.interactable = Player.ReadVariable(Var_StatsEnabled).AsBool();
            FindButton("Notes").Button.interactable = Player.ReadVariable(Var_NotesEnabled).AsBool();
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
            ReevaluateVisibleButtons();

            ClockIncrements.Populate(TimeClock, (int) Player.TimeRemaining());
        }

        private void OnStatsUpdated(int[] adjusted) {
            if (DebugService.AutoTesting) {
                return;
            }

            bool hasAdd = false, hasSubtract = false;
            for(int i = 0; i < Stats.Count; i++) {
                if (adjusted[i] > 0) {
                    hasAdd = true;
                } else if (adjusted[i] < 0) {
                    hasSubtract = false;
                }
            }

            if (Root.IsShowing()) {
                if (hasAdd || hasSubtract) {
                    StatsEffect.gameObject.SetActive(true);
                    if (hasAdd) {
                        Game.Audio.PlayOneShot("StatIncrease");
                        if (hasSubtract) {
                            GameText.PopulateTextLine(StatsEffect, "<b>+ -</b>", null, default, Assets.Style("stat-increase-decrease"), null);
                        } else {
                            GameText.PopulateTextLine(StatsEffect, "<b>+</b>", null, default, Assets.Style("stat-increase"), null);
                        }
                    } else if (hasSubtract) {
                        Game.Audio.PlayOneShot("StatDecrease");
                        GameText.PopulateTextLine(StatsEffect, "<b>-</b>", null, default, Assets.Style("stat-decrease"), null);
                    }
                    GameText.PrepareTextLine(StatsEffect, 2);
                    m_StatsEffectAnim.Replace(this, GameText.AnimateTextLineEffect(StatsEffect, new Vector2(0, 15), 0.3f, 2)).DelayBy(2.5f);
                }
            }
        }

        private void OnTimeUpdated(Player.TimeUpdateArgs args) {
            m_TimeButton.Button.interactable = args.Units > 0;

            ClockIncrements.Populate(TimeClock, (int) args.Units);

            if (DebugService.AutoTesting) {
                return;
            }

            if (args.Delta < 0 && Root.IsShowing()) {
                TimeEffect.gameObject.SetActive(true);
                GameText.PopulateTextLine(TimeEffect, "-" + GameText.FormatTime((uint) -args.Delta, true), null, default, Assets.Style("time-decrease"), null);
                GameText.PrepareTextLine(TimeEffect, 2);
                m_TimeEffectAnim.Replace(this, GameText.AnimateTextLineEffect(TimeEffect, new Vector2(0, 15), 0.3f, 2));
                Game.Audio.PlayOneShot("ClockTick");
            } else if (args.Delta > 0) {
                IEnumerator tutorial = UISystem.SimpleTutorial("Time");
                if (tutorial != null) {
                    Game.Scripting.Interrupt(tutorial);
                }
            }
        }
    
        private void OnVariableChanged(TableKeyPair keyPair) {
            if (keyPair == Var_StatsEnabled || keyPair == Var_NotesEnabled) {
                ReevaluateVisibleButtons();
            }
        }
    }
}