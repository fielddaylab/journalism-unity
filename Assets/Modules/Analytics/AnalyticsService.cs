#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using BeauUtil;
using BeauUtil.Services;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using FieldDay;
using BeauUtil.Debugger;
using static Journalism.Player;

namespace Journalism
{
    public partial class AnalyticsService : ServiceBehaviour, IDebuggable
    {
        #region Inspector

        [SerializeField, Required] private string m_AppId = "JOURNALISM";
        [SerializeField, Required] private string m_AppVersion = " "; // "1.0"; // TODO: what should this val be?
        [SerializeField] private FirebaseConsts m_Firebase = default(FirebaseConsts);

        private const uint HUB_INDEX = 1;
        private const uint TIME_INDEX = 2;
        private const uint ONCE_INDEX = 3;
        private const uint LOCATION_INDEX = 4;
        private const uint CONTINUE_INDEX = 5;
        private const uint ACTION_INDEX = 6;
        private const uint FALLBACK_INDEX = 7;
        private const uint GENERIC_INDEX = 0; // $choice


        #endregion // Inspector

        #region Logging Variables

        private OGDLog m_Log;

        /* Examples
        [NonSerialized] private StringHash32 m_CurrentJobHash = null;
        [NonSerialized] private string m_CurrentJobName = NoActiveJobId;
        [NonSerialized] private string m_PreviousJobName = NoActiveJobId;
        */

        private bool m_FeedbackInProgress;

        [NonSerialized] private bool m_Debug;


        #endregion // Logging Variables

        private void Awake() {
            Initialize();
        }

        #region IService

        protected override void Initialize() // TODO: Where to actually initialize this?
        {
            m_FeedbackInProgress = false;

            // General Events
            Game.Events.Register(GameEvents.StoryEvalBegin, OnFeedbackBegin, this)
                .Register(GameEvents.StoryEvalEnd, OnFeedbackEnd, this)
                .Register<uint>(GameEvents.ChoiceCompleted, OnChoiceCompleted, this);
            // also handles LogHubChoiceClick
            // also handles LogTimeChoiceClick
            // also handles LogLocationChoiceClick
            // also handles LogOnceChoiceClick
            // also handles LogContinueChoiceClick
            // also handles LogActionChoiceClick
            // also handles LogFallbackhoiceClick

            // Analytics Events
            // text click
            Game.Events.Register<TextNodeParams>(GameEvents.TextClicked, LogTextClick, this)
            // display text dialog
                .Register<TextNodeParams>(GameEvents.DisplayTextDialog, LogDisplayTextDialog, this)
                // also handles LogDisplayFeedbackDialog
                // display breakdown dialog
                .Register<StoryStats>(GameEvents.DisplayBreakdownDialog, LogDisplayBreakdownDialog, this)
            // display snippet quality dialog
                .Register<StoryStats>(GameEvents.DisplaySnippetQualityDialog, LogDisplaySnippetQualityDialog, this)
            // display choices
                .Register(GameEvents.DisplayChoices, LogDisplayChoices, this)
            // open stats tab
                .Register(GameEvents.OpenStatsTab, LogOpenStatsTab, this)
            // close stats tab
                .Register(GameEvents.CloseStatsTab, LogCloseStatsTab, this)
            // open map tab
                .Register(GameEvents.OpenMapTab, LogOpenMapTab, this)
            // open choice map
                .Register(GameEvents.OpenChoiceMap, LogOpenChoiceMap, this)
            // close map tab
                .Register(GameEvents.CloseMapTab, LogCloseMapTab, this)
            // open impact map
                .Register(GameEvents.StoryEvalImpact, LogOpenImpactMap, this)
            // close impact map
                .Register(GameEvents.StoryEvalEnd, LogCloseImpactMap, this)
            // reached checkpoint
                .Register(GameEvents.LevelCheckpoint, LogLevelCheckpoint, this)
            // stat update
                .Register<int[]>(GameEvents.StatsUpdated, LogStatsUpdated, this)
            // change background image
                .Register(GameEvents.ChangeBackgroundImage, LogChangeBackgroundImage, this)
            // show popup image
                .Register(GameEvents.ShowPopupImage, LogShowPopupImage, this)
            // change location
                .Register(GameEvents.LocationUpdated, LogLocationUpdated, this)
            // unlocked notebook
                .Register(GameEvents.UnlockedNotebook, LogUnlockedNotebook, this)
            // open notebook
                .Register(GameEvents.OpenNotebook, LogOpenNotebook, this)
            // select snippet
                .Register(GameEvents.SelectSnippet, LogSelectSnippet, this)
            // place snippet
                .Register(GameEvents.PlaceSnippet, LogPlaceSnippet, this)
            // remove snippet
                .Register(GameEvents.RemoveSnippet, LogRemoveSnippet, this)
            // open editor note
                .Register(GameEvents.EditorNotesOpen, LogEditorNotesOpen, this)
            // close editor note
                .Register(GameEvents.EditorNotesClose, LogEditorNotesClose, this)
            // close notebook
                .Register(GameEvents.CloseNotebook, LogCloseNotebook, this)
            // time limit assigned  
                .Register<TimeUpdateArgs>(GameEvents.TimeLimitAssigned, LogTimeLimitAssigned, this)
            // open timer
                .Register(GameEvents.OpenTimer, LogOpenTimer, this)
            // close timer
                .Register(GameEvents.CloseTimer, LogCloseTimer, this)
            // time elapsed
                .Register<TimeUpdateArgs>(GameEvents.TimeElapsed, LogTimeElapsed, this)
            // time expired
                .Register(GameEvents.TimeExpired, LogTimeExpired, this)
            // snippet received  
                .Register(GameEvents.SnippetReceived, LogSnippetReceived, this)
            // story updated
                .Register(GameEvents.StoryUpdated, LogStoryUpdated, this)
            // publish story click
                .Register(GameEvents.StoryPublished, LogPublishStoryClick, this)
            // display published story
                .Register(GameEvents.DisplayPublishedStory, LogDisplayPublishedStory, this)
            // close published story
                .Register(GameEvents.ClosePublishedStory, LogClosePublishedStory, this)
            // start level
                .Register(GameEvents.LevelStarted, LogLevelStarted, this)
            // complete level
                .Register(GameEvents.CompleteLevel, LogCompleteLevel, this)
            // start endgame
                .Register(GameEvents.StartEndgame, LogStartEndgame, this);


            // SceneHelper.OnSceneLoaded += LogSceneChanged;

            // CrashHandler.OnCrash += OnCrash;

            // NetworkStats.OnError.Register(OnNetworkError);

            m_Log = new OGDLog(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = 3 // TODO: what should this val be?
            });
            m_Log.UseFirebase(m_Firebase);

            #if DEVELOPMENT
                m_Debug = true;
            #endif // DEVELOPMENT

            m_Log.SetDebug(m_Debug);
        }

        private void SetUserCode(string userCode)
        {
            m_Log.Initialize(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = 3,
                AppBranch = BuildInfo.Branch()
            });
            m_Log.SetUserId(userCode);
        }

        protected override void Shutdown()
        {
            Game.Events?.DeregisterAll(this);
        }
        #endregion // IService

        #region Log Events


        // text click
        private void LogTextClick(TextNodeParams args) {
            Debug.Log("[Analytics] event: text_click");

            /*
            using (var e = m_Log.NewEvent("text_click")) {
                e.Param("node_id", args.NodeId);
                e.Param("text_content", args.Content);
                e.Param("speaker", args.Speaker);
            }
            */
        }

        // display text dialog
        private void LogDisplayTextDialog(TextNodeParams args) {
            if (m_FeedbackInProgress /*&& speaker == dionne*/) {
                // feedback dialog
                LogDisplayFeedbackDialog();
            }
            else {
                // generic text dialog
                Debug.Log("[Analytics] event: display_text_dialog");

                /*
                using (var e = m_Log.NewEvent("display_text_dialog")) {
                    e.Param("node_id", args.NodeId);
                    e.Param("text_content", args.Content);
                    e.Param("speaker", args.Speaker);
                }
                */
            }
        }

        // display breakdown dialog
        private void LogDisplayBreakdownDialog(StoryStats playerStats) {
            Debug.Log("[Analytics] event: display_breakdown_dialog");

        }

        // display snippet quality dialog
        private void LogDisplaySnippetQualityDialog(StoryStats playerStats) {
            Debug.Log("[Analytics] event: display_snippet_quality_dialog");

        }

        // display feedback dialog
        private void LogDisplayFeedbackDialog() {
            Debug.Log("[Analytics] event: display_feedback_dialog");
        }

        // display choices
        private void LogDisplayChoices() {
            Debug.Log("[Analytics] event: display_choices");

        }

        // hub choice click
        private void LogHubChoiceClick() {
            Debug.Log("[Analytics] event: hub_choice_click");

        }

        // time choice click
        private void LogTimeChoiceClick() {
            Debug.Log("[Analytics] event: time_choice_click");

        }

        // location choice click
        private void LogLocationChoiceClick() {
            Debug.Log("[Analytics] event: location_choice_click");

        }

        // once choice click
        private void LogOnceChoiceClick() {
            Debug.Log("[Analytics] event: once_choice_click");

        }

        // continue choice click
        private void LogContinueChoiceClick() {
            Debug.Log("[Analytics] event: continue_choice_click");

        }

        // action choice click
        private void LogActionChoiceClick() {
            Debug.Log("[Analytics] event: action_choice_click");

        }

        // fallback choice click
        private void LogFallbackChoiceClick() {
            Debug.Log("[Analytics] event: fallback_choice_click");

        }

        // open stats tab
        private void LogOpenStatsTab() {
            Debug.Log("[Analytics] event: open_stats_tab");

        }

        // close stats tab
        private void LogCloseStatsTab() {
            Debug.Log("[Analytics] event: close_stats_tab");

        }

        // open map tab
        private void LogOpenMapTab() {
            Debug.Log("[Analytics] event: open_map_tab");

        }

        // open choice map
        private void LogOpenChoiceMap() {
            Debug.Log("[Analytics] event: open_choice_map");

        }

        // close map tab
        private void LogCloseMapTab() {
            Debug.Log("[Analytics] event: close_map_tab");

        }

        // open impact map
        private void LogOpenImpactMap() {
            Debug.Log("[Analytics] event: open_impact_map");

        }

        // close impact map
        private void LogCloseImpactMap() {
            Debug.Log("[Analytics] event: close_impact_map");

        }

        // reached checkpoint
        private void LogLevelCheckpoint() {
            Debug.Log("[Analytics] event: reached_checkpoint");

        }

        // stat update
        private void LogStatsUpdated(int[] adjustments) {
            Debug.Log("[Analytics] event: stat_update");

        }

        // change background image
        private void LogChangeBackgroundImage() {
            Debug.Log("[Analytics] event: change_background_image");

        }

        // show popup image
        private void LogShowPopupImage() {
            Debug.Log("[Analytics] event: show_popup-image");

        }

        // change location
        private void LogLocationUpdated() {
            Debug.Log("[Analytics] event: change_location");

        }

        // unlocked notebook
        private void LogUnlockedNotebook() {
            Debug.Log("[Analytics] event: unlocked_notebook");

        }

        // open notebook
        private void LogOpenNotebook() {
            Debug.Log("[Analytics] event: open_notebook");

        }

        // select snippet
        private void LogSelectSnippet() {
            Debug.Log("[Analytics] event: select_snippet");

        }

        // place snippet
        private void LogPlaceSnippet() {
            Debug.Log("[Analytics] event: place_snippet");

        }

        // remove snippet
        private void LogRemoveSnippet() {
            Debug.Log("[Analytics] event: remove_snippet");

        }

        // open editor note
        private void LogEditorNotesOpen() {
            Debug.Log("[Analytics] event: editor_notes_open");

        }

        // close editor note
        private void LogEditorNotesClose() {
            Debug.Log("[Analytics] event: editor_notes_close");

        }

        // close notebook
        private void LogCloseNotebook() {
            Debug.Log("[Analytics] event: close_notebook");

        }

        // time limit assigned   
        private void LogTimeLimitAssigned(TimeUpdateArgs args) {
            Debug.Log("[Analytics] event: time_limit_assigned");

        }

        // open timer
        private void LogOpenTimer() {
            Debug.Log("[Analytics] event: open_timer");

        }

        // close timer
        private void LogCloseTimer() {
            Debug.Log("[Analytics] event: close_timer");

        }

        // time elapsed
        private void LogTimeElapsed(TimeUpdateArgs args) {
            Debug.Log("[Analytics] event: time_elapsed");

        }

        // time expired
        private void LogTimeExpired() {
            Debug.Log("[Analytics] event: time_expired");

        }

        // snippet received 
        private void LogSnippetReceived() {
            Debug.Log("[Analytics] event: snippet_received");

        }

        // story updated
        private void LogStoryUpdated() {
            Debug.Log("[Analytics] event: story_updated");

        }

        // publish story click
        private void LogPublishStoryClick() {
            Debug.Log("[Analytics] event: story_click");

        }

        // display published story
        private void LogDisplayPublishedStory() {
            Debug.Log("[Analytics] event: display_published_story");

        }

        // close published story
        private void LogClosePublishedStory() {
            Debug.Log("[Analytics] event: close_published_story");

        }

        // start level
        private void LogLevelStarted() {
            Debug.Log("[Analytics] event: start_level");

        }

        // complete level
        private void LogCompleteLevel() {
            Debug.Log("[Analytics] event: complete_level");

        }

        // start endgame
        private void LogStartEndgame() {
            Debug.Log("[Analytics] event: start_endgame");

        }


        /*
        private void OnCrash(Exception exception, string error) {
            string text = exception != null ? exception.Message : error;
            using(var e = m_Log.NewEvent("game_error")) {
                e.Param("error_message", text);
                e.Param("scene", SceneHelper.ActiveScene().Name);
                e.Param("time_since_launch", Time.realtimeSinceStartup, 2);
                e.Param("job_name", m_CurrentJobName);
            }
            m_Log.Flush();
        }
        */

        private void OnNetworkError(string url) {
            if (url.Length > 480) {
                url = url.Substring(0, 477) + "...";
            }
            using(var e = m_Log.NewEvent("load_error")) {
                e.Param("url", url);
            }
        }

        private void OnChoiceCompleted(uint flavorIndex) {
            switch (flavorIndex) {
                case HUB_INDEX: // hub
                    LogHubChoiceClick();
                    break;
                case TIME_INDEX: // time
                    LogTimeChoiceClick();
                    break;
                case ONCE_INDEX: // once
                    LogOnceChoiceClick();
                    break;
                case LOCATION_INDEX: // location
                    LogLocationChoiceClick();
                    break;
                case CONTINUE_INDEX: // continue
                    LogContinueChoiceClick();
                    break;
                case ACTION_INDEX: // action
                    LogActionChoiceClick();
                    break;
                case FALLBACK_INDEX: // fallback
                    LogFallbackChoiceClick();
                    break;
                case GENERIC_INDEX:
                    // log generic choices as action clicks
                    LogActionChoiceClick();
                    break;
                default:
                    break;
            }
        }

        #endregion // Log Events

        #region Other Events

        private void OnFeedbackBegin() {
            m_FeedbackInProgress = true;
        }

        private void OnFeedbackEnd() {
            m_FeedbackInProgress = false;
        }

        #endregion /// Other Events

#if DEVELOPMENT

        IEnumerable<DMInfo> IDebuggable.ConstructDebugMenus() {
            DMInfo menu = new DMInfo("Analytics", 1);
            menu.AddToggle("Enable Logging", () => {
                return m_Debug;
            }, (t) => {
                m_Debug = t;
                m_Log.SetDebug(t);
            });
            yield return menu;
        }

        #endif // DEVELOPMENT
    }
}
