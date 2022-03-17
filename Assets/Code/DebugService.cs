#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using EasyAssetStreaming;
using Journalism.UI;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Journalism {
    public sealed class DebugService : MonoBehaviour {
#if DEVELOPMENT

        #region Static

        [ServiceReference, UnityEngine.Scripting.Preserve] static private DebugService s_Instance;

        static private DMInfo s_RootMenu;

        #endregion // Static

        #region Inspector

        [SerializeField, Required] private Canvas m_Canvas = null;
        [SerializeField] private KeyCode m_ToggleMinimalKey = KeyCode.BackQuote;
        [SerializeField, Required] private CanvasGroup m_MinimalLayer = null;
        [SerializeField, Required] private DMMenuUI m_DebugMenu = null;
        [SerializeField, Required] private TMP_Text m_StreamingDebugText = null;

        #endregion // Inspector

        [NonSerialized] private bool m_MinimalOn;
        [NonSerialized] private bool m_FirstMenuToggle;
        [NonSerialized] private bool m_Paused;
        [NonSerialized] private float m_TimeScale = 1;
        [NonSerialized] private bool m_VisibilityWhenDebugMenuOpened;
        [NonSerialized] private uint m_LastKnownStreamingCount;
        [NonSerialized] private long m_LastKnownStreamingMem;

        private void Start() {
            s_Instance = this;

            s_RootMenu = new DMInfo("Debug Menu");

            DMInfo levelMenu = new DMInfo("Levels", Assets.LevelCount);
            foreach(var level in Assets.AllLevels) {
                RegisterLoadLevel(levelMenu, level);
            }

            s_RootMenu.AddSubmenu(levelMenu);

            DMInfo statsMenu = new DMInfo("Stats", Stats.Count);
            RegisterAdjustStat(statsMenu, StatId.Research);
            statsMenu.AddDivider();
            RegisterAdjustStat(statsMenu, StatId.Resourceful);
            statsMenu.AddDivider();
            RegisterAdjustStat(statsMenu, StatId.Endurance);
            statsMenu.AddDivider();
            RegisterAdjustStat(statsMenu, StatId.Tech);
            statsMenu.AddDivider();
            RegisterAdjustStat(statsMenu, StatId.Social);
            statsMenu.AddDivider();
            RegisterAdjustStat(statsMenu, StatId.Trust);

            s_RootMenu.AddSubmenu(statsMenu);

            s_RootMenu.AddDivider();

            s_RootMenu.AddToggle("Toggle Toolbar", () => UISystem.GetHeaderEnabled(), (b) => UISystem.SetHeaderEnabled(b));

            #if !UNITY_EDITOR
            SetMinimalLayer(false);
            #else
            SetMinimalLayer(true);
            #endif // PREVIEW
        }

        private void LateUpdate() {
            CheckInput();

            if (m_DebugMenu.isActiveAndEnabled)
                m_DebugMenu.UpdateElements();

            if (m_MinimalOn) {
                bool bUpdated = Ref.Replace(ref m_LastKnownStreamingCount, Streaming.LoadCount());
                bUpdated |= Ref.Replace(ref m_LastKnownStreamingMem, Streaming.TextureMemoryUsage().Current);
                if (bUpdated) {
                    m_StreamingDebugText.SetText(string.Format("{0} / {1:0.00}MB", m_LastKnownStreamingCount, (double)m_LastKnownStreamingMem / (1024 * 1024)));
                }
            }
        }

        private void CheckInput() {
            if (Input.GetKeyDown(m_ToggleMinimalKey)) {
                SetMinimalLayer(!m_MinimalOn);
            }

            UpdateMenuControls();

            if (Input.GetKey(KeyCode.LeftShift)) {
                if (Input.GetKeyDown(KeyCode.Minus)) {
                    SetTimescale(m_TimeScale / 2);
                } else if (Input.GetKeyDown(KeyCode.Equals)) {
                    if (m_TimeScale * 2 < 100)
                        SetTimescale(m_TimeScale * 2);
                } else if (Input.GetKeyDown(KeyCode.Alpha0)) {
                    SetTimescale(1);
                }
            }
        }

        private void UpdateMenuControls() {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.W)) {
                if (m_DebugMenu.isActiveAndEnabled) {
                    m_DebugMenu.gameObject.SetActive(false);
                    Resume();
                    SetMinimalLayer(m_VisibilityWhenDebugMenuOpened);
                } else {
                    if (!m_FirstMenuToggle) {
                        m_DebugMenu.GotoMenu(s_RootMenu);
                        m_FirstMenuToggle = true;
                    }
                    m_DebugMenu.gameObject.SetActive(true);
                    m_VisibilityWhenDebugMenuOpened = m_MinimalOn;
                    SetMinimalLayer(true);
                    Pause();
                }
            }

            if (m_DebugMenu.isActiveAndEnabled) {
                if (Input.GetMouseButtonDown(1))
                    m_DebugMenu.TryPopMenu();
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                    m_DebugMenu.TryPreviousPage();
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                    m_DebugMenu.TryNextPage();
            }
        }

        private void SetTimescale(float inTimeScale) {
            m_TimeScale = inTimeScale;
            if (!m_Paused)
                SyncTimeScale();
        }

        private void SyncTimeScale() {
            Time.timeScale = m_TimeScale;
        }

        private void SetMinimalLayer(bool inbOn) {
            m_MinimalOn = inbOn;
            m_MinimalLayer.alpha = inbOn ? 1 : 0;
            m_MinimalLayer.blocksRaycasts = inbOn;
            m_Canvas.enabled = inbOn;

            if (!inbOn) {
                if (m_DebugMenu.isActiveAndEnabled) {
                    m_DebugMenu.gameObject.SetActive(false);
                    Resume();
                }
            }
        }

        static private void RegisterLoadLevel(DMInfo menu, LevelDef def) {
            menu.AddButton("Load " + def.name, () => {
                s_Instance.SetMinimalLayer(false);
                Game.Scripting.LoadLevel(def, true).OnComplete(() => {
                    Game.Scripting.StartLevel();
                });
            });
        }

        static private void RegisterAdjustStat(DMInfo menu, StatId statId) {
            var info = Stats.Info(statId);
            menu.AddButton(info.Name + " + 1", () => {
                Player.SetStat(statId, Player.Stat(statId) + 1);
            }, () => Player.Stat(statId) < Stats.MaxValue);
            menu.AddButton(info.Name + " - 1", () => {
                Player.SetStat(statId, Player.Stat(statId) - 1);
            }, () => Player.Stat(statId) > 1);
        }

        #region Pausing

        private void Pause() {
            if (m_Paused)
                return;

            Time.timeScale = 0;
            Routine.Settings.Paused = true;
            m_Paused = true;
        }

        private void Resume() {
            if (!m_Paused)
                return;

            SyncTimeScale();
            Routine.Settings.Paused = false;
            m_Paused = false;
        }

        #endregion // Pausing

        #region Asset Reloading

        #endregion // Asset Reloading

        private void HideMenu() {
            if (m_DebugMenu.isActiveAndEnabled) {
                m_DebugMenu.gameObject.SetActive(false);
                Resume();
            }
        }

        #endif // DEVELOPMENT
    }
}