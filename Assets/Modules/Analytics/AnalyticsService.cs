#if UNITY_EDITOR || DEVELOPMENT_BUILD
#define DEVELOPMENT
#endif // UNITY_EDITOR || DEVELOPMENT_BUILD

using BeauUtil;
using System;
using System.Collections.Generic;
using UnityEngine;
using FieldDay;
using BeauUtil.Debugger;
using static Journalism.Player;
using Journalism.UI;
using BeauUtil.Tags;

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

        #region Logging Enums & Structs

        private enum ChoiceContext {
            CONVERSATION,
            LOCATION_MAP
        }


        private struct SnippetDetails {
            public string SnippetId;
            public StoryScrapType StoryScrapType;
            public StoryScrapQuality StoryScrapQuality;
            public List<StoryScrapAttribute> StoryScrapAttributes;
            public bool IsSelectable;

            public SnippetDetails(string inId, StoryScrapType inType, StoryScrapQuality inQuality, List<StoryScrapAttribute> inAttributes, bool inSelectable) {
                SnippetId = inId;
                StoryScrapType = inType;
                StoryScrapQuality = inQuality;
                StoryScrapAttributes = inAttributes;
                IsSelectable = inSelectable;
            }

            public override string ToString() {
                string str = "snippet_id : " + SnippetId + "\n";
                str += "snippet_type : " + StoryScrapType.ToString() + "\n";
                str += "snippet_quality : " + StoryScrapQuality.ToString() + "\n";
                str += "snippet_attributes : " + StoryScrapAttributes.ToString() + "\n";
                str += "is_selectable : " + IsSelectable.ToString() + "\n";

                return str;
            }
        }

        private struct LayoutDetails
        {
            public StoryScrapType ScrapType;
            public bool IsWide;
            public string SnippetId;

            public LayoutDetails(StoryScrapType inType, bool inIsWide, string inId = null) {
                ScrapType = inType;
                IsWide = inIsWide;
                SnippetId = inId;
            }

            public override string ToString() {
                string str = "type : " + ScrapType.ToString() + "\n";
                str += "is_wide : " + IsWide.ToString() + "\n";
                str += "assigned_snippet : " + (SnippetId == null ? "N/A" : SnippetId.ToString()) + "\n";

                return str;
            }
        }


        #endregion // Logging Structs

        #region Logging Variables

        private OGDLog m_Log;

        /* Examples
        [NonSerialized] private StringHash32 m_CurrentJobHash = null;
        [NonSerialized] private string m_CurrentJobName = NoActiveJobId;
        [NonSerialized] private string m_PreviousJobName = NoActiveJobId;
        */

        private bool m_FeedbackInProgress;
        private StringHash32 m_LastKnownNodeId;
        private string m_LastKnownSpeaker;
        private string m_LastKnownNodeContent;
        private PlayerData m_LastKnownPlayerData;
        private StringHash32[] m_LastKnownChoiceLocations;
        private List<StoryBuilderSlot> m_LastKnownSlotLayout;

        private StoryConfig m_TargetBreakdown;
        private StoryStats m_CurrentBreakdown;

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
                .Register<TextChoice>(GameEvents.ChoiceCompleted, OnChoiceCompleted, this)
            // also handles LogHubChoiceClick
            // also handles LogTimeChoiceClick
            // also handles LogLocationChoiceClick
            // also handles LogOnceChoiceClick
            // also handles LogContinueChoiceClick
            // also handles LogActionChoiceClick
            // also handles LogFallbackchoiceClick
                .Register<TextNodeParams>(GameEvents.OnPrepareLine, OnPrepareLine, this)
                .Register<PlayerData>(GameEvents.StatsRefreshed, OnStatsRefreshed, this)
                .Register<StringHash32[]>(GameEvents.ChoiceOptionsUpdating, OnChoiceOptionsUpdating)
                .Register<List<StoryBuilderSlot>>(GameEvents.SlotsLaidOut, OnSlotsLaidOut);


            // Analytics Events
            // text click
            Game.Events.Register(GameEvents.TextClicked, LogTextClick, this)
            // display text dialog
                .Register(GameEvents.DisplayTextDialog, LogDisplayTextDialog, this)
                // also handles LogDisplayFeedbackDialog
                // display breakdown dialog
                .Register(GameEvents.DisplayBreakdownDialog, LogDisplayBreakdownDialog, this)
            // display snippet quality dialog
                .Register<StoryStats>(GameEvents.DisplaySnippetQualityDialog, LogDisplayStoryScrapQualityDialog, this)
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
                .Register<RingBuffer<ImpactLayout.Item>>(GameEvents.StoryImpactDisplayed, LogOpenImpactMap, this)
            // close impact map
                .Register(GameEvents.StoryEvalEnd, LogCloseImpactMap, this)
            // reached checkpoint
                .Register(GameEvents.LevelCheckpoint, LogLevelCheckpoint, this)
            // stat update
                .Register<int[]>(GameEvents.StatsUpdated, LogStatsUpdated, this)
            // change background image
                .Register<string>(GameEvents.ChangeBackgroundImage, LogChangeBackgroundImage, this)
            // show popup image
                .Register<TagEventData>(GameEvents.ShowPopupImage, LogShowPopupImage, this)
            // change location
                .Register<StringHash32>(GameEvents.LocationUpdated, LogLocationUpdated, this)
            // unlocked notebook
                .Register(GameEvents.UnlockedNotebook, LogUnlockedNotebook, this)
            // open notebook
                .Register(GameEvents.OpenNotebook, LogOpenNotebook, this)
            // select snippet
                .Register<StoryScrapData>(GameEvents.SelectSnippet, LogSelectSnippet, this)
            // place snippet
                .Register<StoryBuilderSlot>(GameEvents.PlaceSnippet, LogPlaceSnippet, this)
            // remove snippet
                .Register<StoryBuilderSlot>(GameEvents.RemoveSnippet, LogRemoveSnippet, this)
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
                .Register<StringHash32>(GameEvents.InventoryUpdated, LogSnippetReceived, this)
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
        private void LogTextClick() {
            Debug.Log("[Analytics] event: text_click" + "\n    node id: " + m_LastKnownNodeId + " || content: " + m_LastKnownNodeContent + " || speaker: " + m_LastKnownSpeaker);
            
            /*
            using (var e = m_Log.NewEvent("text_click")) {
                e.Param("node_id", args.NodeId);
                e.Param("text_content", args.Content);
                e.Param("speaker", args.Speaker);
            }
            */
        }

        // display text dialog
        private void LogDisplayTextDialog() {
            if (m_FeedbackInProgress /*&& speaker == dionne*/) {
                // feedback dialog
                LogDisplayFeedbackDialog();
            }
            else {
                // generic text dialog
                Debug.Log("[Analytics] event: display_text_dialog" + "\n    node id:: " + m_LastKnownNodeId + " || content: " + m_LastKnownNodeContent + " || speaker: " + m_LastKnownSpeaker);

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
        private void LogDisplayBreakdownDialog() {
            Debug.Log("[Analytics] event: display_breakdown_dialog");

            m_TargetBreakdown = Assets.CurrentLevel.Story;
            m_CurrentBreakdown = Player.StoryStatistics;

            Dictionary<string, int> final_breakdown = new Dictionary<string, int>();
            final_breakdown.Add("color_weight", m_CurrentBreakdown.ColorCount);
            final_breakdown.Add("facts_weight", m_CurrentBreakdown.FactCount);
            final_breakdown.Add("useful_weight", m_CurrentBreakdown.UsefulCount);

            Dictionary<string, int> target_breakdown = new Dictionary<string, int>();
            target_breakdown.Add("color_weight", m_TargetBreakdown.ColorWeight);
            target_breakdown.Add("facts_weight", m_TargetBreakdown.FactWeight);
            target_breakdown.Add("useful_weight", m_TargetBreakdown.UsefulWeight);

        }

        // display snippet quality dialog
        private void LogDisplayStoryScrapQualityDialog(StoryStats playerStats) {
            Debug.Log("[Analytics] event: display_snippet_quality_dialog");

            List<StoryScrapQuality> current_quality = GenerateStoryScrapQualityList();
        }

        // display feedback dialog
        private void LogDisplayFeedbackDialog() {
            Debug.Log("[Analytics] event: display_feedback_dialog");

            string node_id = m_LastKnownNodeId.ToString();
            string text_content = m_LastKnownNodeContent;
            float storyScore = m_CurrentBreakdown.TotalQuality;
            float story_alignment = m_CurrentBreakdown.Alignment;
        }

        // display choices
        private void LogDisplayChoices() {
            Debug.Log("[Analytics] event: display_choices");

            ChoiceContext context;

            // TODO: journalism schema indicates this section is a TODO
            // List<>
        }

        // hub choice click
        private void LogHubChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: hub_choice_click");

            string text_content = m_LastKnownNodeContent;
            string node_id = m_LastKnownNodeId.ToString(); // TODO: change to current_node_id for consistency?
            string next_node_id = choice.TargetId.ToString();
            string next_location = Assets.Location(choice.LocationId).Name; // optional
            int time_cost = (int)choice.TimeCost;
            bool time_cost_is_mystery = choice.QuestionMark.IsActive() && choice.QuestionMark.enabled;
        }

        // ------------------- RESUME WORKING HERE -------------------

        // time choice click
        private void LogTimeChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: time_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
            string next_node_id = choice.TargetId.ToString();
            int time_cost = (int)choice.TimeCost;
            bool time_cost_is_mystery = choice.QuestionMark.IsActive() && choice.QuestionMark.enabled;

        }

        // location choice click
        private void LogLocationChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: location_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
            string next_node_id = choice.TargetId.ToString();
            string next_location = Assets.Location(choice.LocationId).Name;
        }

        // once choice click
        private void LogOnceChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: once_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
            string next_node_id = choice.TargetId.ToString();
        }

        // continue choice click
        private void LogContinueChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: continue_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
        }

        // action choice click
        private void LogActionChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: action_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
        }

        // fallback choice click
        private void LogFallbackChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: fallback_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
            string next_node_id = choice.TargetId.ToString();
        }

        // open stats tab
        private void LogOpenStatsTab() {
            Debug.Log("[Analytics] event: open_stats_tab");

            // no event data
        }

        // close stats tab
        private void LogCloseStatsTab() {
            Debug.Log("[Analytics] event: close_stats_tab");

            // no event data
        }

        // open map tab
        private void LogOpenMapTab() {
            Debug.Log("[Analytics] event: open_map_tab");

            string current_location = Assets.Location(Player.Location()).Name;
            List<string> locations_list = new List<string>(); // locations currently displayed on the map
            foreach(var id in m_LastKnownChoiceLocations) {
                locations_list.Add(Assets.Location(id).Name);
            }


        }

        // open choice map
        private void LogOpenChoiceMap() {
            Debug.Log("[Analytics] event: open_choice_map");

            string current_location = Assets.Location(Player.Location()).Name;
            List<string> locations_list = new List<string>(); // locations currently displayed on the map
            foreach (var id in m_LastKnownChoiceLocations) {
                locations_list.Add(Assets.Location(id).Name);
            }


        }

        // close map tab
        private void LogCloseMapTab() {
            Debug.Log("[Analytics] event: close_map_tab");

            // no event data
        }

        // open impact map
        private void LogOpenImpactMap(RingBuffer<ImpactLayout.Item> feedbackItems) {
            Debug.Log("[Analytics] event: open_impact_map");

            List<string> feedback_ids = new List<string>();
            List<string> feedback_texts = new List<string>();

            foreach(ImpactLayout.Item item in feedbackItems) {
                feedback_ids.Add(item.SnippetId.ToString());
                feedback_texts.Add(item.RichText);
            }
        }

        // close impact map
        private void LogCloseImpactMap() {
            Debug.Log("[Analytics] event: close_impact_map");

            // no event data
        }

        // reached checkpoint
        private void LogLevelCheckpoint() {
            Debug.Log("[Analytics] event: reached_checkpoint");

            string node_id = m_LastKnownNodeId.ToString();
        }

        // stat update
        private void LogStatsUpdated(int[] adjustments) {
            Debug.Log("[Analytics] event: stat_update");

            Dictionary<StatId, int> updatedStats = new Dictionary<StatId, int>();

            for (int i = 0; i < Stats.Count; i++) {
                int adjust = adjustments[i];
                updatedStats.Add((StatId)i, adjust);
            }

            string updateStatsStr = "ENDURANCE : " + updatedStats[StatId.Endurance] + "\n";
            updateStatsStr += "RESOURCEFUL : " + updatedStats[StatId.Resourceful] + "\n";
            updateStatsStr += "TECH : " + updatedStats[StatId.Tech] + "\n";
            updateStatsStr += "SOCIAL : " + updatedStats[StatId.Social] + "\n";
            updateStatsStr += "TRUST : " + updatedStats[StatId.Trust] + "\n";
            updateStatsStr += "RESEARCH : " + updatedStats[StatId.Research] + "\n";


        }

        // change background image
        private void LogChangeBackgroundImage(string path) {
            Debug.Log("[Analytics] event: change_background_image: " + path);

            string node_id = m_LastKnownNodeId.ToString();
            string image_name = ParseFileName(path);

        }

        // show popup image
        private void LogShowPopupImage(TagEventData evtData) {
            Debug.Log("[Analytics] event: show_popup-image");

            var args = evtData.ExtractStringArgs();
            string path = args[0].ToString();

            bool is_animated = (path.Contains(".webm") || path.Contains(".mp4"));
            string node_id = m_LastKnownNodeId.ToString();
            string image_name = ParseFileName(path);

        }

        // change location
        private void LogLocationUpdated(StringHash32 locId) {
            Debug.Log("[Analytics] event: change_location");

            string new_location_id = Assets.Location(locId).Name;


        }

        // unlocked notebook
        private void LogUnlockedNotebook() {
            Debug.Log("[Analytics] event: unlocked_notebook");

            // no event data
        }

        // open notebook
        private void LogOpenNotebook() {
            Debug.Log("[Analytics] event: open_notebook");

            List<SnippetDetails> snippet_list = new List<SnippetDetails>();

            // TODO: adding dicts as params?
            string snippetDetailsStr = "";

            foreach(SnippetDetails snippet in snippet_list) {
                snippetDetailsStr += snippet.ToString();
            }

            List<LayoutDetails> layout_list = new List<LayoutDetails>();

            string layoutDetailsStr = "";

            foreach (LayoutDetails layout in layout_list) {
                layoutDetailsStr += layout.ToString();
            }

            // e.Param snippetDetailsStr
            // e.Param layoutDetailsStr
        }

        // select snippet
        private void LogSelectSnippet(StoryScrapData snippetData) {
            Debug.Log("[Analytics] event: select_snippet");

            string snippet_id = snippetData.Id.ToString();
            StoryScrapType snippet_type = snippetData.Type;
            StoryScrapQuality snippet_quality = snippetData.Quality;
            // List<StoryScrapAttribute> snippet_attributes = new List<StoryScrapAttribute>();

            string attributesStr = GenerateAttributesString(snippetData);


        }

        // place snippet
        private void LogPlaceSnippet(StoryBuilderSlot slot) {
            Debug.Log("[Analytics] event: place_snippet");

            List<LayoutDetails> layout_list = new List<LayoutDetails>();
            foreach(StoryBuilderSlot s in m_LastKnownSlotLayout) {
                layout_list.Add(new LayoutDetails(
                    s.Data.Type,
                    m_TargetBreakdown.Slots[slot.Index].Wide,
                    s.Data.Id.ToString()));
            }

            string layoutDetailsStr = "";

            foreach (LayoutDetails layout in layout_list) {
                layoutDetailsStr += layout.ToString();
            }


            int location = slot.Index;
            string snippet_id = slot.Data.Id.ToString();
            string snippet_type = slot.Data.Type.ToString();
            string snippet_quality = slot.Data.Quality.ToString();

            // List<StoryScrapAttribute> snippet_attributes = new List<StoryScrapAttribute>();

            string attributesStr = GenerateAttributesString(slot.Data);

        }

        // remove snippet
        private void LogRemoveSnippet(StoryBuilderSlot slot) {
            Debug.Log("[Analytics] event: remove_snippet");

            List<LayoutDetails> layout_list = new List<LayoutDetails>();
            foreach (StoryBuilderSlot s in m_LastKnownSlotLayout) {
                layout_list.Add(new LayoutDetails(
                    s.Data.Type,
                    m_TargetBreakdown.Slots[slot.Index].Wide,
                    s.Data.Id.ToString()));
            }

            string layoutDetailsStr = "";

            foreach (LayoutDetails layout in layout_list) {
                layoutDetailsStr += layout.ToString();
            }


            int location = slot.Index;
            string snippet_id = slot.Data.Id.ToString();
            string snippet_type = slot.Data.Type.ToString();
            string snippet_quality = slot.Data.Quality.ToString();

            // List<StoryScrapAttribute> snippet_attributes = new List<StoryScrapAttribute>();

            string attributesStr = GenerateAttributesString(slot.Data);


        }

        // open editor note
        private void LogEditorNotesOpen() {
            Debug.Log("[Analytics] event: editor_notes_open");

            m_TargetBreakdown = Assets.CurrentLevel.Story;
            m_CurrentBreakdown = Player.StoryStatistics;

            Dictionary<string, int> current_breakdown = new Dictionary<string, int>();
            current_breakdown.Add("color_weight", m_CurrentBreakdown.ColorCount);
            current_breakdown.Add("facts_weight", m_CurrentBreakdown.FactCount);
            current_breakdown.Add("useful_weight", m_CurrentBreakdown.UsefulCount);

            Dictionary<string, int> target_breakdown = new Dictionary<string, int>();
            target_breakdown.Add("color_weight", m_TargetBreakdown.ColorWeight);
            target_breakdown.Add("facts_weight", m_TargetBreakdown.FactWeight);
            target_breakdown.Add("useful_weight", m_TargetBreakdown.UsefulWeight);

            List<StoryScrapQuality> current_quality = GenerateStoryScrapQualityList();


        }

        // close editor note
        private void LogEditorNotesClose() {
            Debug.Log("[Analytics] event: editor_notes_close");

            // no event data
        }

        // close notebook
        private void LogCloseNotebook() {
            Debug.Log("[Analytics] event: close_notebook");

            // no event data
        }

        // time limit assigned   
        private void LogTimeLimitAssigned(TimeUpdateArgs args) {
            Debug.Log("[Analytics] event: time_limit_assigned");

            string node_id = m_LastKnownNodeId.ToString();
            float time_delta = args.Delta;
        }

        // open timer
        private void LogOpenTimer() {
            Debug.Log("[Analytics] event: open_timer");

            float time_left = Player.TimeRemaining();
        }

        // close timer
        private void LogCloseTimer() {
            Debug.Log("[Analytics] event: close_timer");

            // no event data
        }

        // time elapsed
        private void LogTimeElapsed(TimeUpdateArgs args) {
            Debug.Log("[Analytics] event: time_elapsed");

            string node_id = m_LastKnownNodeId.ToString();
            int how_much = args.Delta; // in minutes


        }

        // time expired
        private void LogTimeExpired() {
            Debug.Log("[Analytics] event: time_expired");

            string node_id = m_LastKnownNodeId.ToString();
            int leftover_time = 0; // in minutes // TODO: revisit premise. Time doesn't expire with leftover time, there are just limits on options
        
        
        }

        // snippet received 
        private void LogSnippetReceived(StringHash32 snippetId) {
            Debug.Log("[Analytics] event: snippet_received");

            StoryScrapData snippetData = Assets.Scrap(snippetId);
            string node_id = m_LastKnownNodeId.ToString();
            string snippet_id = snippetId.ToString();
            string snippet_type = snippetData.Type.ToString();
            string snippet_quality = snippetData.Quality.ToString();

            GenerateAttributesString(snippetData);
        }

        // story updated
        private void LogStoryUpdated() {
            Debug.Log("[Analytics] event: story_updated");

            m_TargetBreakdown = Assets.CurrentLevel.Story;
            m_CurrentBreakdown = Player.StoryStatistics;

            Dictionary<string, int> new_breakdown = new Dictionary<string, int>();
            new_breakdown.Add("color_weight", m_CurrentBreakdown.ColorCount);
            new_breakdown.Add("facts_weight", m_CurrentBreakdown.FactCount);
            new_breakdown.Add("useful_weight", m_CurrentBreakdown.UsefulCount);

            Dictionary<string, int> target_breakdown = new Dictionary<string, int>();
            target_breakdown.Add("color_weight", m_TargetBreakdown.ColorWeight);
            target_breakdown.Add("facts_weight", m_TargetBreakdown.FactWeight);
            target_breakdown.Add("useful_weight", m_TargetBreakdown.UsefulWeight);

            List<StoryScrapQuality> new_quality = GenerateStoryScrapQualityList();


        }

        // publish story click
        private void LogPublishStoryClick() {
            Debug.Log("[Analytics] event: story_click");

            List<SnippetDetails> snippet_list = new List<SnippetDetails>();

            // TODO: adding dicts as params?
            string snippetDetailsStr = "";

            foreach (SnippetDetails snippet in snippet_list) {
                snippetDetailsStr += snippet.ToString();
            }


            List<LayoutDetails> layout_list = new List<LayoutDetails>();

            string layoutDetailsStr = "";

            foreach (LayoutDetails layout in layout_list) {
                layoutDetailsStr += layout.ToString();
            }

            // e.Param snippetDetailsStr
            // e.Param layoutDetailsStr

        }

        // display published story
        private void LogDisplayPublishedStory() {
            Debug.Log("[Analytics] event: display_published_story");

            // TODO: revisit schema -- seems like it should mirror event above with LayoutDetails, instead of defining a new struct
        }

        // close published story
        private void LogClosePublishedStory() {
            Debug.Log("[Analytics] event: close_published_story");

            // no event data
        }

        // start level
        private void LogLevelStarted() {
            Debug.Log("[Analytics] event: start_level");

            int level_started = Assets.CurrentLevel.LevelIndex;


        }

        // complete level
        private void LogCompleteLevel() {
            Debug.Log("[Analytics] event: complete_level");

            int level_completed = Assets.CurrentLevel.LevelIndex;
        }

        // start endgame
        private void LogStartEndgame() {
            Debug.Log("[Analytics] event: start_endgame");

            float city_score = Player.Data.CityScore;

            int scenario; // 1, 2,or 3 // TODO: make enum instead?

            if (Player.Data.CityScore > 2) {
                scenario = 1; // great
            }
            else if (Player.Data.CityScore > 0) {
                scenario = 2; // good
            }
            else {
                scenario = 3; // bad
            }


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

        private void OnChoiceCompleted(TextChoice choice) {
            switch (choice.ChoiceType) {
                case HUB_INDEX: // hub
                    LogHubChoiceClick(choice);
                    break;
                case TIME_INDEX: // time
                    LogTimeChoiceClick(choice);
                    break;
                case ONCE_INDEX: // once
                    LogOnceChoiceClick(choice);
                    break;
                case LOCATION_INDEX: // location
                    LogLocationChoiceClick(choice);
                    break;
                case CONTINUE_INDEX: // continue
                    LogContinueChoiceClick(choice);
                    break;
                case ACTION_INDEX: // action
                    LogActionChoiceClick(choice);
                    break;
                case FALLBACK_INDEX: // fallback
                    LogFallbackChoiceClick(choice);
                    break;
                case GENERIC_INDEX:
                    // log generic choices as action clicks
                    LogActionChoiceClick(choice);
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

        private void OnPrepareLine(TextNodeParams args) {
            m_LastKnownNodeId = args.NodeId;
            m_LastKnownNodeContent = args.Content;
            m_LastKnownSpeaker = args.Speaker;
        }

        private void OnStatsRefreshed(PlayerData data) {
            m_LastKnownPlayerData = data;
        }

        private void OnChoiceOptionsUpdating(StringHash32[] locIds) {
            m_LastKnownChoiceLocations = locIds;
        }

        private void OnSlotsLaidOut(List<StoryBuilderSlot> activeSlots) {
            m_LastKnownSlotLayout = activeSlots;
        }

        #endregion // Other Events

        #region Helpers

        private string ParseFileName(string path) {
            while (path.Contains("/")) {
                int cutIndex = path.IndexOf('/');
                path = path.Substring(cutIndex + 1);
            }
            int extIndex = path.IndexOf('.');

            return path.Substring(0, extIndex);
        }

        private string GenerateAttributesString(StoryScrapData snippetData) {
            List<StoryScrapAttribute> snippet_attributes = new List<StoryScrapAttribute>();

            string attributesStr = "";

            if ((snippetData.Attributes & StoryScrapAttribute.Facts) != 0) {
                snippet_attributes.Add(StoryScrapAttribute.Facts);
            }
            if ((snippetData.Attributes & StoryScrapAttribute.Color) != 0) {
                snippet_attributes.Add(StoryScrapAttribute.Color);
            }
            if ((snippetData.Attributes & StoryScrapAttribute.Useful) != 0) {
                snippet_attributes.Add(StoryScrapAttribute.Useful);
            }

            foreach (StoryScrapAttribute attr in snippet_attributes) {
                attributesStr += attr.ToString() + "\n";
            }

            return attributesStr;
        } 

        private List<StoryScrapQuality> GenerateStoryScrapQualityList() {
            List<StoryScrapQuality> qualities = new List<StoryScrapQuality>();

            foreach (var scrapId in m_LastKnownPlayerData.AllocatedScraps) {
                if (!scrapId.IsEmpty) {
                    StoryScrapData scrapData = Assets.Scrap(scrapId);
                    qualities.Add(scrapData.Quality);
                }
            }

            return qualities;
        }

        #endregion // Helpers

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
