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
using BeauPools;
using Leaf;
using Newtonsoft.Json;

namespace Journalism.Analytics
{
    public enum FailType {
        Time,
        Choice,
        Research,
        Resourceful,
        Endurance,
        Tech,
        Social,
        Trust
    }

    public enum ResumedCheckpointOrigin {
        Menu,
        LevelFail
    }

    public partial class AnalyticsService : ServiceBehaviour, IDebuggable
    {
        #region Inspector

        [SerializeField, Required] private string m_AppId = "JOURNALISM";
        [SerializeField, Required] private string m_AppVersion = "1.0";
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

        [Serializable]
        public struct SnippetDetails {
            public string SnippetId;
            public StoryScrapType StoryScrapType;
            public StoryScrapQuality StoryScrapQuality;
            public string StoryScrapAttributesList; // format of a list
            public bool IsSelectable;

            public SnippetDetails(string inId, StoryScrapType inType, StoryScrapQuality inQuality, string inAttributesList, bool inSelectable) {
                SnippetId = inId;
                StoryScrapType = inType;
                StoryScrapQuality = inQuality;
                StoryScrapAttributesList = inAttributesList;
                IsSelectable = inSelectable;
            }

            public override string ToString() {
                string str = "snippet_id : " + SnippetId + "\n";
                str += "snippet_type : " + StoryScrapType.ToString() + "\n";
                str += "snippet_quality : " + StoryScrapQuality.ToString() + "\n";
                str += "snippet_attributes : " + StoryScrapAttributesList + "\n";
                str += "is_selectable : " + IsSelectable.ToString() + "\n";

                return str;
            }
        }

        [Serializable]
        public struct LayoutDetails
        {
            [SerializeField] public StoryScrapType ScrapType;
            [SerializeField] public bool IsWide;
            [SerializeField] public string SnippetId;

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

        private bool m_FeedbackInProgress;
        private string m_LastKnownNodeId;
        private string m_LastKnownSpeaker;
        private string m_LastKnownNodeContent;
        private PlayerData m_LastKnownPlayerData;
        private StringHash32[] m_LastKnownChoiceLocations;
        private List<StoryBuilderSlot> m_LastKnownSlotLayout;

        private StoryConfig m_TargetBreakdown;
        private StoryStats m_CurrentBreakdown;

        private bool m_TextMapVisible;

        [NonSerialized] private bool m_Debug;


        #endregion // Logging Variables

        private void Awake() {
            Initialize();
        }

        #region IService

        protected override void Initialize()
        {
            m_FeedbackInProgress = false;
            m_TextMapVisible = false;

            m_LastKnownChoiceLocations = null;

            m_LastKnownSlotLayout = new List<StoryBuilderSlot>();

            // General Events
            Game.Events.Register(GameEvents.StoryEvalBegin, OnFeedbackBegin, this)
                .Register<string>(GameEvents.ProfileStarting, SetUserCode, this)
                .Register(GameEvents.StoryEvalEnd, OnFeedbackEnd, this)
                .Register<TextChoice>(GameEvents.ChoiceCompleted, OnChoiceCompleted, this)
                .Register<TextNodeParams>(GameEvents.OnPrepareLine, OnPrepareLine, this)
                .Register<NodeNameParams>(GameEvents.OnNodeStart, OnNodeStart, this)
                .Register<PlayerData>(GameEvents.StatsRefreshed, OnStatsRefreshed, this)
                .Register<StringHash32[]>(GameEvents.ChoiceOptionsUpdating, OnChoiceOptionsUpdating)
                .Register<List<StoryBuilderSlot>>(GameEvents.SlotsLaidOut, OnSlotsLaidOut)
                .Register(GameText.Events.Map, OnTextMap)
                .Register(GameText.Events.ClearMap, OnTextMapClear);


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
                .Register<PooledList<LeafChoice.Option>>(GameEvents.DisplayChoices, LogDisplayChoices, this)
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
                .Register(GameEvents.StartEndgame, LogStartEndgame, this)
            // start new game
                .Register(GameEvents.NewGameSuccess, LogNewGameSuccess, this)
            // game over soon
                .Register<List<FailType>>(GameEvents.ImminentFailure, LogImminentFailure, this)
            // resumed checkpoint
                .Register<ResumedCheckpointOrigin>(GameEvents.ResumedCheckpoint, LogResumedCheckpoint, this);


            // SceneHelper.OnSceneLoaded += LogSceneChanged;

            // CrashHandler.OnCrash += OnCrash;

            // NetworkStats.OnError.Register(OnNetworkError);

            m_Log = new OGDLog(new OGDLogConsts() {
                AppId = m_AppId,
                AppVersion = m_AppVersion,
                ClientLogVersion = 1
            });
            m_Log.UseFirebase(m_Firebase);

            #if DEVELOPMENT
                m_Debug = true;
            #endif // DEVELOPMENT

            m_Log.SetDebug(m_Debug);
        }

        private void SetUserCode(string userCode)
        {
            Debug.Log("[Analytics] Setting user code: " + userCode);
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
            
            using (var e = m_Log.NewEvent("text_click")) {
                e.Param("node_id", m_LastKnownNodeId.ToString());
                e.Param("text_content", m_LastKnownNodeContent);
                e.Param("speaker", m_LastKnownSpeaker);
            }
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

                using (var e = m_Log.NewEvent("display_text_dialog")) {
                    e.Param("node_id", m_LastKnownNodeId.ToString());
                    e.Param("text_content", m_LastKnownNodeContent);
                    e.Param("speaker", m_LastKnownSpeaker);
                }
            }
        }

        // display breakdown dialog
        private void LogDisplayBreakdownDialog() {
            Debug.Log("[Analytics] event: display_breakdown_dialog");

            m_TargetBreakdown = Assets.CurrentLevel.Story;
            m_CurrentBreakdown = StoryStats.FromPlayerData(Player.Data, m_TargetBreakdown);

            Dictionary<string, int> final_breakdown = new Dictionary<string, int> {
                { "color_weight", m_CurrentBreakdown.ColorCount },
                { "facts_weight", m_CurrentBreakdown.FactCount },
                { "useful_weight", m_CurrentBreakdown.UsefulCount }
            };

            Dictionary<string, int> target_breakdown = new Dictionary<string, int> {
                { "color_weight", m_TargetBreakdown.ColorWeight },
                { "facts_weight", m_TargetBreakdown.FactWeight },
                { "useful_weight", m_TargetBreakdown.UsefulWeight }
            };

            using (var e = m_Log.NewEvent("display_breakdown_dialog")) {
                e.Param("final_breakdown", JsonConvert.SerializeObject(final_breakdown));
                e.Param("target_breakdown", JsonConvert.SerializeObject(target_breakdown));
            }
        }

        // display snippet quality dialog
        private void LogDisplayStoryScrapQualityDialog(StoryStats playerStats) {
            Debug.Log("[Analytics] event: display_snippet_quality_dialog");

            List<StoryScrapQuality> current_quality = GenerateStoryScrapQualityList();

            using (var e = m_Log.NewEvent("display_snippet_quality_dialog")) {
                e.Param("final_breakdown", JsonConvert.SerializeObject(current_quality));
            }
        }

        // display feedback dialog
        private void LogDisplayFeedbackDialog() {
            Debug.Log("[Analytics] event: display_feedback_dialog");

            m_CurrentBreakdown = StoryStats.FromPlayerData(Player.Data, Assets.CurrentLevel.Story);

            string node_id = m_LastKnownNodeId.ToString();
            string text_content = m_LastKnownNodeContent;
            float story_score = m_CurrentBreakdown.TotalQuality;
            float story_alignment = m_CurrentBreakdown.Alignment;
            // Debug.Log("[Analytics]      SCORE: " + story_score);
            // Debug.Log("[Analytics]      ALIGNMENT: " + story_alignment);


            using (var e = m_Log.NewEvent("display_feedback_dialog")) {
                e.Param("node_id", node_id);
                e.Param("text_content", text_content);
                e.Param("story_score", story_score);
                e.Param("story_alignment", story_alignment);
            }
        }

        // display choices
        private void LogDisplayChoices(PooledList<LeafChoice.Option> fullOptions) {
            Debug.Log("[Analytics] event: display_choices");

            ChoiceContext context = m_TextMapVisible ? ChoiceContext.LOCATION_MAP : ChoiceContext.CONVERSATION;

            // TODO: journalism schema indicates this section is a TODO
            List<string> choices = new List<string>();

            foreach(var option in fullOptions) {
                choices.Add(option.TargetId.ToString());
            }

            using (var e = m_Log.NewEvent("display_choices")) {
                e.Param("context", context.ToString());
                e.Param("choices", JsonConvert.SerializeObject(choices));
            }
        }

        // hub choice click
        private void LogHubChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: hub_choice_click");

            string text_content = m_LastKnownNodeContent;
            string node_id = m_LastKnownNodeId.ToString(); // TODO: change to current_node_id for consistency?
            string next_node_id = choice.TargetId.ToString();
            string next_location = "";
            if (!choice.LocationId.IsEmpty) {
                next_location = Assets.Location(choice.LocationId).Name; // optional
            }
            int time_cost = (int)choice.TimeCost;
            bool time_cost_is_mystery = choice.QuestionMark.IsActive() && choice.QuestionMark.enabled;

            using (var e = m_Log.NewEvent("hub_choice_click")) {
                e.Param("text_content", text_content);
                e.Param("node_id", node_id);
                e.Param("next_node_id", next_node_id);
                if (!choice.LocationId.IsEmpty) { e.Param("next_location", next_location); } // optional
                e.Param("time_cost", time_cost);
                e.Param("time_cost_is_mystery", time_cost_is_mystery);
            }
        }

        // time choice click
        private void LogTimeChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: time_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
            string next_node_id = choice.TargetId.ToString();
            int time_cost = (int)choice.TimeCost;
            bool time_cost_is_mystery = choice.QuestionMark.IsActive() && choice.QuestionMark.enabled;

            using (var e = m_Log.NewEvent("time_choice_click")) {
                e.Param("text_content", text_content);
                e.Param("current_node_id", current_node_id);
                e.Param("next_node_id", next_node_id);
                e.Param("time_cost", time_cost);
                e.Param("time_cost_is_mystery", time_cost_is_mystery);
            }
        }

        // location choice click
        private void LogLocationChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: location_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
            string next_node_id = choice.TargetId.ToString();
            string next_location = choice.LocationId.IsEmpty ? "N/A" : Assets.Location(choice.LocationId).Name;

            using (var e = m_Log.NewEvent("location_choice_click")) {
                e.Param("text_content", text_content);
                e.Param("current_node_id", current_node_id);
                e.Param("next_node_id", next_node_id);
                e.Param("next_location", next_location);
            }
        }

        // once choice click
        private void LogOnceChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: once_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
            string next_node_id = choice.TargetId.ToString();

            using (var e = m_Log.NewEvent("once_choice_click")) {
                e.Param("text_content", text_content);
                e.Param("current_node_id", current_node_id);
                e.Param("next_node_id", next_node_id);
            }
        }

        // continue choice click
        private void LogContinueChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: continue_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();

            using (var e = m_Log.NewEvent("continue_choice_click")) {
                e.Param("text_content", text_content);
                e.Param("current_node_id", current_node_id);
            }
        }

        // action choice click
        private void LogActionChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: action_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();

            using (var e = m_Log.NewEvent("action_choice_click")) {
                e.Param("text_content", text_content);
                e.Param("current_node_id", current_node_id);
            }
        }

        // fallback choice click
        private void LogFallbackChoiceClick(TextChoice choice) {
            Debug.Log("[Analytics] event: fallback_choice_click");

            string text_content = m_LastKnownNodeContent;
            string current_node_id = m_LastKnownNodeId.ToString();
            string next_node_id = choice.TargetId.ToString();


            using (var e = m_Log.NewEvent("fallback_choice_click")) {
                e.Param("text_content", text_content);
                e.Param("current_node_id", current_node_id);
                e.Param("next_node_id", next_node_id);
            }
        }

        // open stats tab
        private void LogOpenStatsTab() {
            Debug.Log("[Analytics] event: open_stats_tab");

            // no event data
            m_Log.NewEvent("open_stats_tab");
        }

        // close stats tab
        private void LogCloseStatsTab() {
            Debug.Log("[Analytics] event: close_stats_tab");

            // no event data
            m_Log.NewEvent("close_stats_tab");
        }

        // open map tab
        private void LogOpenMapTab() {
            Debug.Log("[Analytics] event: open_map_tab");

            string current_location = Assets.Location(Player.Location()).Name;
            List<string> locations_list = new List<string>(); // locations currently displayed on the map
            if (m_LastKnownChoiceLocations != null) {
                foreach (StringHash32 id in m_LastKnownChoiceLocations) {
                    string locStr = (id.IsEmpty || id == null) ? "N/A" : Assets.Location(id).Name;
                    locations_list.Add(locStr);
                }
            }

            using (var e = m_Log.NewEvent("open_map_tab")) {
                e.Param("current_location", current_location);
                e.Param("locations_list", JsonConvert.SerializeObject(locations_list));
            }

        }

        // open choice map
        private void LogOpenChoiceMap() {
            Debug.Log("[Analytics] event: open_choice_map");

            string current_location = Assets.Location(Player.Location()).Name;
            List<string> locations_list = new List<string>(); // locations currently displayed on the map
            if (m_LastKnownChoiceLocations != null) {
                foreach (StringHash32 id in m_LastKnownChoiceLocations) {
                    string locStr = (id.IsEmpty || id == null) ? "N/A" : Assets.Location(id).Name;
                    locations_list.Add(locStr);
                }
            }

            using (var e = m_Log.NewEvent("open_choice_map")) {
                e.Param("current_location", current_location);
                e.Param("locations_list", JsonConvert.SerializeObject(locations_list));
            }
        }

        // close map tab
        private void LogCloseMapTab() {
            Debug.Log("[Analytics] event: close_map_tab");

            // no event data
            m_Log.NewEvent("close_map_tab");
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

            using (var e = m_Log.NewEvent("open_impact_map")) {
                e.Param("feeback_ids", JsonConvert.SerializeObject(feedback_ids));
                e.Param("feedback_texts", JsonConvert.SerializeObject(feedback_texts));
            }
        }

        // close impact map
        private void LogCloseImpactMap() {
            Debug.Log("[Analytics] event: close_impact_map");

            // no event data
            m_Log.NewEvent("close_impact_map");
        }

        // reached checkpoint
        private void LogLevelCheckpoint() {
            Debug.Log("[Analytics] event: reached_checkpoint");

            string node_id = m_LastKnownNodeId.ToString();

            using (var e = m_Log.NewEvent("reached_checkpoint")) {
                e.Param("node_id", node_id);
            }
        }

        // stat update
        private void LogStatsUpdated(int[] adjustments) {
            UpdateGameState();

            Debug.Log("[Analytics] event: stat_update");

            string node_id = m_LastKnownNodeId.ToString();

            Dictionary<StatId, int> updatedStats = new Dictionary<StatId, int>();

            for (int i = 0; i < Stats.Count; i++) {
                int adjust = adjustments[i];
                updatedStats.Add((StatId)i, adjust);
            }

            using (var e = m_Log.NewEvent("stat_update")) {
                e.Param("node_id", node_id);
                e.Param("stats", JsonConvert.SerializeObject(updatedStats));
            }
        }

        // change background image
        private void LogChangeBackgroundImage(string path) {
            Debug.Log("[Analytics] event: change_background_image: " + path);

            string node_id = m_LastKnownNodeId.ToString();
            string image_name = ParseFileName(path);

            using (var e = m_Log.NewEvent("change_background_image")) {
                e.Param("node_id", node_id);
                e.Param("image_name", image_name);
            }
        }

        // show popup image
        private void LogShowPopupImage(TagEventData evtData) {
            Debug.Log("[Analytics] event: show_popup_image");

            var args = evtData.ExtractStringArgs();
            string path = args[0].ToString();

            bool is_animated = (path.Contains(".webm") || path.Contains(".mp4"));
            string node_id = m_LastKnownNodeId.ToString();
            string image_name = ParseFileName(path);

            using (var e = m_Log.NewEvent("show_popup_image")) {
                e.Param("is_animated", is_animated);
                e.Param("node_id", node_id);
                e.Param("image_name", image_name);
            }
        }

        // change location
        private void LogLocationUpdated(StringHash32 locId) {
            UpdateGameState();
            Debug.Log("[Analytics] event: change_location");

            string new_location_id = locId.IsEmpty ? "N/A" : Assets.Location(locId).Name;

            using (var e = m_Log.NewEvent("change_location")) {
                e.Param("new_location_id", new_location_id);
            }
        }

        // unlocked notebook
        private void LogUnlockedNotebook() {
            Debug.Log("[Analytics] event: unlocked_notebook");

            // no event data
            m_Log.NewEvent("unlocked_notebook");
        }

        // open notebook
        private void LogOpenNotebook() {
            Debug.Log("[Analytics] event: open_notebook");

            List<SnippetDetails> snippet_list = new List<SnippetDetails>();

            // first 25 snippets to prevent overflow
            for (int s = 0; s < Player.StoryScraps.Length && s < 25; s++) {
                StoryScrapData sData = Assets.Scrap(Player.StoryScraps[s]);
                string attributeStr = GenerateAttributesString(sData);
                SnippetDetails newDetails = new SnippetDetails(Player.StoryScraps[s].ToString(), sData.Type, sData.Quality, attributeStr, true);
                snippet_list.Add(newDetails);
            }

            List<LayoutDetails> layout_list = new List<LayoutDetails>();

            foreach (StoryBuilderSlot s in m_LastKnownSlotLayout) {
                if (s.Data == null || m_TargetBreakdown.Slots == null) {
                    continue;
                }
                layout_list.Add(new LayoutDetails(
                    s.Data.Type,
                    m_TargetBreakdown.Slots[s.Index].Wide,
                    s.Data.Id.ToString()));
            }

            using (var e = m_Log.NewEvent("open_notebook")) {
                e.Param("snippet_list", JsonConvert.SerializeObject(snippet_list));
                e.Param("layout", JsonConvert.SerializeObject(layout_list));
            }
        }

        // select snippet
        private void LogSelectSnippet(StoryScrapData snippetData) {
            Debug.Log("[Analytics] event: select_snippet");

            string snippet_id = snippetData.Id.ToString();
            StoryScrapType snippet_type = snippetData.Type;
            StoryScrapQuality snippet_quality = snippetData.Quality;
            // List<StoryScrapAttribute> snippet_attributes = new List<StoryScrapAttribute>();

            string attributesStr = GenerateAttributesString(snippetData);

            using (var e = m_Log.NewEvent("select_snippet")) {
                e.Param("snippet_id", snippet_id);
                e.Param("snippet_type", snippet_type.ToString());
                e.Param("snippet_quality", snippet_quality.ToString());
                e.Param("snippet_attributes", attributesStr);
            }
        }

        // place snippet
        private void LogPlaceSnippet(StoryBuilderSlot slot) {
            Debug.Log("[Analytics] event: place_snippet");

            List<LayoutDetails> layout_list = new List<LayoutDetails>();
            foreach(StoryBuilderSlot s in m_LastKnownSlotLayout) {
                if (s.Data == null || m_TargetBreakdown.Slots == null) {
                    continue;
                }
                layout_list.Add(new LayoutDetails(
                    s.Data.Type,
                    m_TargetBreakdown.Slots[s.Index].Wide,
                    s.Data.Id.ToString()));
            }


            int location = slot.Index;
            string snippet_id = slot.Data.Id.ToString();
            string snippet_type = slot.Data.Type.ToString();
            string snippet_quality = slot.Data.Quality.ToString();

            string attributesStr = GenerateAttributesString(slot.Data);

            using (var e = m_Log.NewEvent("place_snippet")) {
                e.Param("layout", JsonConvert.SerializeObject(layout_list));
                e.Param("location", location);
                e.Param("snippet_id", snippet_id);
                e.Param("snippet_type", snippet_type);
                e.Param("snippet_quality", snippet_quality);
                e.Param("snippet_attribute", attributesStr);
            }
        }

        // remove snippet
        private void LogRemoveSnippet(StoryBuilderSlot slot) {
            Debug.Log("[Analytics] event: remove_snippet");

            List<LayoutDetails> layout_list = new List<LayoutDetails>();
            foreach (StoryBuilderSlot s in m_LastKnownSlotLayout) {
                if (s.Data == null || m_TargetBreakdown.Slots == null) {
                    continue;
                }
                layout_list.Add(new LayoutDetails(
                    s.Data.Type,
                    m_TargetBreakdown.Slots[s.Index].Wide,
                    s.Data.Id.ToString()));
            }

            int location = slot.Index;
            string snippet_id = slot.Data.Id.ToString();
            string snippet_type = slot.Data.Type.ToString();
            string snippet_quality = slot.Data.Quality.ToString();

            string attributesStr = GenerateAttributesString(slot.Data);

            using (var e = m_Log.NewEvent("remove_snippet")) {
                e.Param("layout", JsonConvert.SerializeObject(layout_list));
                e.Param("location", location);
                e.Param("snippet_id", snippet_id);
                e.Param("snippet_type", snippet_type);
                e.Param("snippet_quality", snippet_quality);
                e.Param("snippet_attribute", attributesStr);
            }
        }

        // open editor note
        private void LogEditorNotesOpen() {
            Debug.Log("[Analytics] event: editor_notes_open");

            m_TargetBreakdown = Assets.CurrentLevel.Story;
            m_CurrentBreakdown = StoryStats.FromPlayerData(Player.Data, m_TargetBreakdown);
            // Debug.Log("[Analytics]      SCORE: " + story_score);
            // Debug.Log("[Analytics]      ALIGNMENT: " + story_alignment);

            Dictionary<string, int> current_breakdown = new Dictionary<string, int>();
            current_breakdown.Add("color_weight", m_CurrentBreakdown.ColorCount);
            current_breakdown.Add("facts_weight", m_CurrentBreakdown.FactCount);
            current_breakdown.Add("useful_weight", m_CurrentBreakdown.UsefulCount);

            Dictionary<string, int> target_breakdown = new Dictionary<string, int>();
            target_breakdown.Add("color_weight", m_TargetBreakdown.ColorWeight);
            target_breakdown.Add("facts_weight", m_TargetBreakdown.FactWeight);
            target_breakdown.Add("useful_weight", m_TargetBreakdown.UsefulWeight);

            List<StoryScrapQuality> current_quality = GenerateStoryScrapQualityList();

            using (var e = m_Log.NewEvent("editor_notes_open")) {
                e.Param("current_breakdown", current_breakdown.ToString());
                e.Param("target_breakdown", target_breakdown.ToString());
                e.Param("current_quality", current_quality.ToString());
            }
        }

        // close editor note
        private void LogEditorNotesClose() {
            Debug.Log("[Analytics] event: editor_notes_close");

            // no event data
            m_Log.NewEvent("editor_notes_close");
        }

        // close notebook
        private void LogCloseNotebook() {
            Debug.Log("[Analytics] event: close_notebook");

            // no event data
            m_Log.NewEvent("close_notebook");
        }

        // time limit assigned   
        private void LogTimeLimitAssigned(TimeUpdateArgs args) {
            Debug.Log("[Analytics] event: time_limit_assigned");

            string node_id = m_LastKnownNodeId.ToString();
            float time_delta = args.Delta;

            using (var e = m_Log.NewEvent("time_limit_assigned")) {
                e.Param("node_id", node_id);
                e.Param("time_delta", time_delta);
            }
        }

        // open timer
        private void LogOpenTimer() {
            Debug.Log("[Analytics] event: open_timer");

            float time_left = Player.TimeRemaining();

            using (var e = m_Log.NewEvent("open_timer")) {
                e.Param("time_left", time_left);
            }
        }

        // close timer
        private void LogCloseTimer() {
            Debug.Log("[Analytics] event: close_timer");

            // no event data
            m_Log.NewEvent("close_timer");
        }

        // time elapsed
        private void LogTimeElapsed(TimeUpdateArgs args) {
            Debug.Log("[Analytics] event: time_elapsed");

            string node_id = m_LastKnownNodeId.ToString();
            int how_much = args.Delta; // in minutes

            using (var e = m_Log.NewEvent("time_elapsed")) {
                e.Param("node_id", node_id);
                e.Param("how_much", how_much);
            }
        }

        // time expired
        private void LogTimeExpired() {
            Debug.Log("[Analytics] event: time_expired");

            string node_id = m_LastKnownNodeId.ToString();
            // TODO: revisit premise of leftover_time. Time doesn't expire with leftover time, there are just limits on options
            int leftover_time = 0; // in minutes 

            using (var e = m_Log.NewEvent("time_expired")) {
                e.Param("node_id", node_id);
                e.Param("leftover_time", leftover_time);
            }
        }

        // snippet received 
        private void LogSnippetReceived(StringHash32 snippetId) {
            Debug.Log("[Analytics] event: snippet_received");

            StoryScrapData snippetData = Assets.Scrap(snippetId);

            string node_id = m_LastKnownNodeId.ToString();
            string snippet_id = snippetId.ToString();
            string snippet_type;
            string snippet_quality;
            /* TODO: somewhere a snippet is null despite having id
            if (snippetData == null) {
                snippet_type = "N/A";
                snippet_quality = "N/A";
            }
            else {
                snippet_type = snippetData.Type.ToString();
                snippet_quality = snippetData.Quality.ToString();
            }
            */
            snippet_type = snippetData.Type.ToString();
            snippet_quality = snippetData.Quality.ToString();

            string attributeStr = GenerateAttributesString(snippetData);

            using (var e = m_Log.NewEvent("snippet_received")) {
                e.Param("node_id", node_id);
                e.Param("snippet_id", snippet_id);
                e.Param("snippet_type", snippet_type);
                e.Param("snippet_quality", snippet_quality);
                e.Param("snippet_attributes", attributeStr);
            }
        }

        // story updated
        private void LogStoryUpdated() {
            Debug.Log("[Analytics] event: story_updated");

            m_TargetBreakdown = Assets.CurrentLevel.Story;
            m_CurrentBreakdown = StoryStats.FromPlayerData(Player.Data, m_TargetBreakdown);
            // Debug.Log("[Analytics]      SCORE: " + story_score);
            // Debug.Log("[Analytics]      ALIGNMENT: " + story_alignment);

            Dictionary<string, int> new_breakdown = new Dictionary<string, int>();
            new_breakdown.Add("color_weight", m_CurrentBreakdown.ColorCount);
            new_breakdown.Add("facts_weight", m_CurrentBreakdown.FactCount);
            new_breakdown.Add("useful_weight", m_CurrentBreakdown.UsefulCount);

            Dictionary<string, int> target_breakdown = new Dictionary<string, int>();
            target_breakdown.Add("color_weight", m_TargetBreakdown.ColorWeight);
            target_breakdown.Add("facts_weight", m_TargetBreakdown.FactWeight);
            target_breakdown.Add("useful_weight", m_TargetBreakdown.UsefulWeight);

            List<StoryScrapQuality> new_quality = GenerateStoryScrapQualityList();

            using (var e = m_Log.NewEvent("story_updated")) {
                e.Param("new_breakdown", JsonConvert.SerializeObject(new_breakdown));
                e.Param("target_breakdown", JsonConvert.SerializeObject(target_breakdown));
                e.Param("new_quality", JsonConvert.SerializeObject(new_quality));
                e.Param("story_score", m_CurrentBreakdown.TotalQuality);
                e.Param("story_alignment", m_CurrentBreakdown.Alignment);
            }
        }

        // publish story click
        private void LogPublishStoryClick() {
            Debug.Log("[Analytics] event: story_click");

            List<SnippetDetails> snippet_list = new List<SnippetDetails>();

            // first 25 to prevent overlow
            for (int s = 0; s < Player.StoryScraps.Length && s < 25; s++) {
                StoryScrapData sData = Assets.Scrap(Player.StoryScraps[s]);
                string attributeStr = GenerateAttributesString(sData);
                SnippetDetails newDetails = new SnippetDetails(Player.StoryScraps[s].ToString(), sData.Type, sData.Quality, attributeStr, true);
                snippet_list.Add(newDetails);
            }

            List<LayoutDetails> layout_list = new List<LayoutDetails>();

            foreach (StoryBuilderSlot s in m_LastKnownSlotLayout) {
                if (s.Data == null || m_TargetBreakdown.Slots == null) {
                    continue;
                }
                layout_list.Add(new LayoutDetails(
                    s.Data.Type,
                    m_TargetBreakdown.Slots[s.Index].Wide,
                    s.Data.Id.ToString()));
            }

            using (var e = m_Log.NewEvent("story_click")) {
                e.Param("snippet_list", JsonConvert.SerializeObject(snippet_list));
                e.Param("layout", JsonConvert.SerializeObject(layout_list));
            }
        }

        // display published story
        private void LogDisplayPublishedStory() {
            Debug.Log("[Analytics] event: display_published_story");

            List<LayoutDetails> layout_list = new List<LayoutDetails>();

            foreach (StoryBuilderSlot s in m_LastKnownSlotLayout) {
                if (s.Data == null || m_TargetBreakdown.Slots == null) {
                    continue;
                }
                layout_list.Add(new LayoutDetails(
                    s.Data.Type,
                    m_TargetBreakdown.Slots[s.Index].Wide,
                    s.Data.Id.ToString())); // schema lists text here, but every other list of layouts uses snippetId
            }

            using (var e = m_Log.NewEvent("display_published_story")) {
                e.Param("story_layout", JsonConvert.SerializeObject(layout_list));
            }
        }

        // close published story
        private void LogClosePublishedStory() {
            Debug.Log("[Analytics] event: close_published_story");

            // no event data
            m_Log.NewEvent("close_published_story");
        }

        // start level
        private void LogLevelStarted() {
            UpdateGameState();

            Debug.Log("[Analytics] event: start_level");

            m_TargetBreakdown = Assets.CurrentLevel.Story;
            m_CurrentBreakdown = StoryStats.FromPlayerData(Player.Data, m_TargetBreakdown);
            // Debug.Log("[Analytics]      SCORE: " + story_score);
            // Debug.Log("[Analytics]      ALIGNMENT: " + story_alignment);

            int level_started = Assets.CurrentLevel.LevelIndex;

            using (var e = m_Log.NewEvent("start_level")) {
                e.Param("level_started", level_started);
            }
        }

        // complete level
        private void LogCompleteLevel() {
            Debug.Log("[Analytics] event: complete_level");

            int level_completed = Assets.CurrentLevel.LevelIndex;

            using (var e = m_Log.NewEvent("complete_level")) {
                e.Param("level_completed", level_completed);
            }
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

            using (var e = m_Log.NewEvent("start_endgame")) {
                e.Param("city_score", city_score);
                e.Param("scenario", scenario);
            }
        }

        private void LogNewGameSuccess() {
            Debug.Log("[Analytics] event: new_game");

            m_Log.NewEvent("new_game");
        }

        private void LogContinueGameSuccess() {
            Debug.Log("[Analytics] event: continue_game");

            m_Log.NewEvent("continue_game");
        }

        private void LogImminentFailure(List<FailType> failTypes) {
            Debug.Log("[Analytics] event: level_fail");

            using (var e = m_Log.NewEvent("level_fail")) {
                e.Param("fail_types", JsonConvert.SerializeObject(failTypes));
            }
        }

        // resumed checkpoint
        private void LogResumedCheckpoint(ResumedCheckpointOrigin origin) {
            Debug.Log("[Analytics] event: resumed_checkpoint at node id " + m_LastKnownNodeId + " from " + origin.ToString());

            string node_id = m_LastKnownNodeId;

            using (var e = m_Log.NewEvent("resumed_checkpoint")) {
                e.Param("node_id", node_id);
                e.Param("origin", origin.ToString());
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
            m_LastKnownNodeContent = args.Content;
            m_LastKnownSpeaker = args.Speaker;
        }

        private void OnNodeStart(NodeNameParams args) {
            m_LastKnownNodeId = args.NodeId;
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

        private void OnTextMap() {
            m_TextMapVisible = true;
        }

        private void OnTextMapClear() {
            m_TextMapVisible = false;
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

            if ((snippetData.Attributes & StoryScrapAttribute.Facts) != 0) {
                snippet_attributes.Add(StoryScrapAttribute.Facts);
            }
            if ((snippetData.Attributes & StoryScrapAttribute.Color) != 0) {
                snippet_attributes.Add(StoryScrapAttribute.Color);
            }
            if ((snippetData.Attributes & StoryScrapAttribute.Useful) != 0) {
                snippet_attributes.Add(StoryScrapAttribute.Useful);
            }

            return JsonConvert.SerializeObject(snippet_attributes);
        } 

        private List<StoryScrapQuality> GenerateStoryScrapQualityList() {
            List<StoryScrapQuality> qualities = new List<StoryScrapQuality>();

            if (m_LastKnownPlayerData == null) {
                return qualities;
            }

            foreach (var scrapId in m_LastKnownPlayerData.AllocatedScraps) {
                if (scrapId != null && !scrapId.IsEmpty) {
                    StoryScrapData scrapData = Assets.Scrap(scrapId);
                    qualities.Add(scrapData.Quality);
                }
            }

            return qualities;
        }

        private void UpdateGameState() {
            string location = Player.Data.LocationId.IsEmpty ? "N/A" : Assets.Location(Player.Data.LocationId).Name;
            m_Log.BeginGameState();
                m_Log.GameStateParam("level", Assets.CurrentLevel.LevelIndex);
                m_Log.GameStateParam("current_stats", JsonConvert.SerializeObject(Player.Data.StatValues));
                m_Log.GameStateParam("location", location);
            m_Log.SubmitGameState();
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
