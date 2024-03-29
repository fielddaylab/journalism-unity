#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BeauPools;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Services;
using EasyAssetStreaming;
using EasyBugReporter;
using Journalism.UI;
using Leaf.Runtime;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Journalism {
    public sealed class DebugService : MonoBehaviour {
        #if DEVELOPMENT

        #region Static

        [ServiceReference, UnityEngine.Scripting.Preserve] static private DebugService s_Instance;

        static private DMInfo s_RootMenu;

        #if UNITY_EDITOR

        /*
            NOTE(Autumn): Okay so this really isn't the best way of handling a sort of auto-tester
            Adding a bunch of "if (DebugService.AutoTesting)" calls in the game code is pretty gross
            But it turns out running a leaf script while the game isn't running is rather tricky
            So this is the hack we've got for the moment. Sorry :(
        */
        static public bool AutoTesting {
            get { return s_Instance.m_AutoTesting; }
        }

        #else

        public const bool AutoTesting = false;

        #endif // UNITY_EDITOR

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
        [NonSerialized] private bool m_AutoTesting;
        [NonSerialized] private bool m_AutoTestErrorFlag;
        [NonSerialized] private Routine m_AutoTestRoutine;
        
        private DumpSourceCollection m_ContextReporters;

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
            RegisterAdjustStat(statsMenu, StatId.Resourceful);
            RegisterAdjustStat(statsMenu, StatId.Endurance);
            RegisterAdjustStat(statsMenu, StatId.Tech);
            RegisterAdjustStat(statsMenu, StatId.Social);
            RegisterAdjustStat(statsMenu, StatId.Trust);

            s_RootMenu.AddSubmenu(statsMenu);

            /* TODO: adjust city score via debug menu
            DMInfo cityScoreMenu = new DMInfo("CityScore",);
            RegisterAdjustStat(statsMenu, StatId.Research);

            s_RootMenu.AddSubmenu(cityScoreMenu);
            */

            s_RootMenu.AddDivider();
            s_RootMenu.AddButton("Unlock All Story Snippets", UnlockAllSnippets);
            s_RootMenu.AddButton("Skip to Story Publish", SkipToStoryPublish);

            s_RootMenu.AddDivider();

            s_RootMenu.AddToggle("Toggle Toolbar", () => UISystem.GetHeaderEnabled(), (b) => UISystem.SetHeaderEnabled(b));
            s_RootMenu.AddToggle("Toggle Story Visible", () => UISystem.GetStoryEnabled(), (b) => UISystem.SetStoryEnabled(b));

            #if UNITY_EDITOR

            s_RootMenu.AddDivider();
            s_RootMenu.AddButton("Auto Test", StartAutoTest, () => !m_AutoTesting);

            #endif // UNITY_EDITOR

            #if !UNITY_EDITOR
            SetMinimalLayer(false);
            #else
            SetMinimalLayer(true);
            #endif // PREVIEW

            m_ContextReporters = new DumpSourceCollection();
            m_ContextReporters.Add(new ScreenshotContext());
            m_ContextReporters.Add(new UnityContext());
            m_ContextReporters.Add(new LogContext());
            m_ContextReporters.Add(new SystemInfoContext());

            m_ContextReporters.Initialize();

            Game.Scripting.OnScriptError += (LeafEvalContext errorContext) => {
                m_AutoTestErrorFlag = true;
            };
            Game.Scripting.OnThreadStopped += () => {
                if (m_AutoTesting) {
                    m_AutoTestErrorFlag = true;
                }
            };
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

            // unity whyyyyyyy
            if (UnityEngine.Debug.developerConsoleVisible) {
                UnityEngine.Debug.developerConsoleVisible = false;
                UnityEngine.Debug.ClearDeveloperConsole();
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

            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F9) && Input.GetKey(KeyCode.LeftControl)) {
                BugReporter.DumpContext(m_ContextReporters);
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
            menu.AddButton(info.Id.ToString() + " + 1", () => {
                Player.SetStat(statId, Player.Stat(statId) + 1);
            }, () => Player.Stat(statId) < Stats.MaxValue);
            menu.AddButton(info.Id.ToString() + " - 1", () => {
                Player.SetStat(statId, Player.Stat(statId) - 1);
            }, () => Player.Stat(statId) > 0);
        }

        static private void UnlockAllSnippets() {
            foreach(var snippet in Assets.CurrentLevel.StoryScraps) {
                Player.Data.StoryScrapInventory.Add(snippet.Id);
            }
        }

        static private void SkipToStoryPublish() {
            Game.Scripting.SkipTo("STORYEVALUATION");
        }

        #if UNITY_EDITOR

        private void StartAutoTest() {
            m_AutoTesting = true;
            m_AutoTestRoutine.Replace(this, AutoTestRoutine());
            SetMinimalLayer(false);
        }

        private IEnumerator AutoTestRoutine() {
            try {
                Log.Msg("[DebugService] Beginning auto-testing...");
                m_AutoTesting = true;
                Time.timeScale = 100;
                m_AutoTestErrorFlag = false;
                int count = 500;
                while(count-- > 0) {
                    m_AutoTestErrorFlag = false;
                    Log.Msg("[DebugService] New auto-test starting");
                    Game.Save.NewLocalSave();
                    var load = Game.Scripting.LoadLevel(0, true);
                    yield return load;
                    yield return null;
                    Game.Scripting.StartLevel();
                    while(!m_AutoTestErrorFlag) {
                        yield return null;
                    }
                    BugReporter.DumpContext(m_ContextReporters, BugReporter.DumpFlags.Silent);
                    while(BugReporter.IsDumping()) {
                        yield return null;
                    }
                }
            } finally {
                m_AutoTesting = false;
                Time.timeScale = 1;
            }
        }

        #endif // UNITY_EDITOR

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

        #else

        public const bool AutoTesting = false;

        private void Start() {
            Destroy(gameObject);
        }

        #endif // DEVELOPMENT
    }
}