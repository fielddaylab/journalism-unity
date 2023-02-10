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
                .Register(GameEvents.DisplayChoices, LogDisplayChoices, this);
            /*
            // open stats tab                   || LogOpenStatsTab()
                .Register(GameEvents.OpenStatsTab, LogOpenStatsTab, this)
            // close stats tab                  || LogCloseStatsTab()
                .Register(GameEvents.CloseStatsTab, LogCloseStatsTab, this)
            // open map tab                     || LogOpenMapTab()
                .Register(GameEvents.OpenMapTab, LogOpenMapTab, this)
            // open choice map                  || LogOpenChoiceMap()
                .Register(GameEvents.OpenChoiceMap, LogOpenChoiceMap, this)
            // close map tab                    || LogCloseMapTab()
                .Register(GameEvents.CloseMapTab, LogCloseMapTab, this)
            // open impact map                  || LogOpenImpactMap()
                .Register(GameEvents.OpenImpactMap, LogOpenImpactMap, this)
            // close impact map                 || LogCloseImpactMap()
                .Register(GameEvents.CloseImpactMap, LogCloseImpactMap, this)
            // reached checkpoint               || LogReachedCheckpoint()
                .Register(GameEvents.LevelCheckpoint, LogLevelCheckpoint, this)
            // stat update                      || LogStatUpdate()
                .Register(GameEvents.StatsUpdated, LogStatsUpdated, this)
            // change background image          || LogChangeBackgroundImage()
                .Register(GameEvents.ChangeBackgroundImage, LogChangeBackgroundImage, this)
            // show popup image                 || LogShowPopupImage()
                .Register(GameEvents.ShowPopupImage, LogShowPopupImage, this)
            // change location                  || LogChangeLocation()
                .Register(GameEvents.LocationUpdated, LogLocationUpdated, this)
            // unlocked notebook                || LogUnlockedNotebook()
                .Register(GameEvents.UnlockedNotebook, LogUnlockedNotebook, this)
            // open notebook                    || LogOpenNotebook()
                .Register(GameEvents.OpenNotebook, LogOpenNotebook, this)
            // select snippet                   || LogSelectSnippet()
                .Register(GameEvents.SelectSnippet, LogSelectSnippet, this)
            // place snippet                    || LogPlaceSnippet()
                .Register(GameEvents.PlaceSnippet, LogPlaceSnippet, this)
            // remove snippet                   || LogRemoveSnippet()
                .Register(GameEvents.RemoveSnippet, LogRemoveSnippet, this)
            // open editor note                 || LogOpenEditorNote()
                .Register(GameEvents.EditorNotesOpen, LogEditorNotesOpen, this)
            // close editor note                || LogCloseEditorNote()
                .Register(GameEvents.EditorNotesClose, LogEditorNotesClose, this)
            // close notebook                   || LogCloseNotebook()
                .Register(GameEvents.CloseNotebook, LogCloseNotebook, this)
            // time limit assigned              || LogTimeLimitAssigned()    
                .Register(GameEvents.TimeUpdated, LogTimeUpdated, this)
            // open timer                       || LogOpenTimer()
                .Register(GameEvents.OpenTimer, LogOpenTimer, this)
            // close timer                      || LogCloseTimer()
                .Register(GameEvents.CloseTimer, LogCloseTimer, this)
            // time elapsed                     || LogTimeElapsed()
                .Register(GameEvents.TimeElapsed, LogTimeElapsed, this)
            // time expired                     || LogTimeExpired()
                .Register(GameEvents.TimeExpired, LogTimeExpired, this)
            // snippet received                 || LogSnippetReceived()    
                .Register(GameEvents.SnippetReceived, LogSnippetReceived, this)
            // story updated                    || LogStoryUpdated()
                .Register(GameEvents.StoryUpdated, LogStoryUpdated, this)
            // publish story click              || LogPublishStoryClick()
                .Register(GameEvents.PublishStoryClick, LogPublishStoryClick, this)
            // display published story          || LogDisplayPublishedStory()
                .Register(GameEvents.DisplayPublishedStory, LogDisplayPublishedStory, this)
            // close published story            || LogClosePublishedStory() 
                .Register(GameEvents.ClosePublishedStory, LogClosePublishedStory, this)
            // start level                      || LogStartLevel()
                .Register(GameEvents.LevelStarted, LogLevelStarted, this)
            // complete level                   || LogCompleteLevel()
                .Register(GameEvents.CompleteLevel, LogCompleteLevel, this)
            // start endgame                    || LogStartEndgame()
                .Register(GameEvents.StartEndgame, LogStartEndgame, this);
                */


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
            }

            /*
            using (var e = m_Log.NewEvent("display_text_dialog")) {
                e.Param("node_id", args.NodeId);
                e.Param("text_content", args.Content);
                e.Param("speaker", args.Speaker);
            }
            */
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

        // open stats tab                   || LogOpenStatsTab()
        private void LogOpenStatsTab() {

        }

        // close stats tab                  || LogCloseStatsTab()
        private void LogCloseStatsTab() {

        }

        // open map tab                     || LogOpenMapTab()
        private void LogOpenMapTab() {

        }

        // open choice map                  || LogOpenChoiceMap()
        private void LogOpenChoiceMap() {

        }

        // close map tab                    || LogCloseMapTab()
        private void LogCloseMapTab() {

        }

        // open impact map                  || LogOpenImpactMap()
        private void LogOpenImpactMap() {

        }

        // close impact map                 || LogCloseImpactMap()
        private void LogCloseImpactMap() {

        }

        // reached checkpoint               || LogReachedCheckpoint()
        private void LogLevelCheckpoint() {

        }

        // stat update                      || LogStatUpdate()
        private void LogStatsUpdated() {

        }

        // change background image          || LogChangeBackgroundImage()
        private void LogChangeBackgroundImage() {

        }

        // show popup image                 || LogShowPopupImage()
        private void LogShowPopupImage() {

        }

        // change location                  || LogChangeLocation()
        private void LogLocationUpdated() {

        }

        // unlocked notebook                || LogUnlockedNotebook()
        private void LogUnlockedNotebook() {

        }

        // open notebook                    || LogOpenNotebook()
        private void LogOpenNotebook() {

        }

        // select snippet                   || LogSelectSnippet()
        private void LogSelectSnippet() {

        }

        // place snippet                    || LogPlaceSnippet()
        private void LogPlaceSnippet() {

        }

        // remove snippet                   || LogRemoveSnippet()
        private void LogRemoveSnippet() {

        }

        // open editor note                 || LogOpenEditorNote()
        private void LogEditorNotesOpen() {

        }

        // close editor note                || LogCloseEditorNote()
        private void LogEditorNotesClose() {

        }

        // close notebook                   || LogCloseNotebook()
        private void LogCloseNotebook() {

        }

        // time limit assigned              || LogTimeLimitAssigned()    
        private void LogTimeUpdated() {

        }

        // open timer                       || LogOpenTimer()
        private void LogOpenTimer() {

        }

        // close timer                      || LogCloseTimer()
        private void LogCloseTimer() {

        }

        // time elapsed                     || LogTimeElapsed()
        private void LogTimeElapsed() {

        }

        // time expired                     || LogTimeExpired()
        private void LogTimeExpired() {

        }

        // snippet received                 || LogSnippetReceived()    
        private void LogSnippetReceived() {

        }

        // story updated                    || LogStoryUpdated()
        private void LogStoryUpdated() {

        }

        // publish story click              || LogPublishStoryClick()
        private void LogPublishStoryClick() {

        }

        // display published story          || LogDisplayPublishedStory()
        private void LogDisplayPublishedStory() {

        }

        // close published story            || LogClosePublishedStory() 
        private void LogClosePublishedStory() {

        }

        // start level                      || LogStartLevel()
        private void LogLevelStarted() {
        
        }

        // complete level                   || LogCompleteLevel()
        private void LogCompleteLevel() {

        }

        // start endgame                    || LogStartEndgame()
        private void LogStartEndgame() {

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
