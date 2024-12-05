using BeauUtil;

namespace Journalism {
    static public class GameEvents {
        static public readonly StringHash32 VariableUpdated = "save:variable-updated"; // TableKeyPair variableId
        static public readonly StringHash32 SaveDeclared = "save:declared"; // PlayerData data
        static public readonly StringHash32 ProfileStarting = "profile:starting"; // string userName
        static public readonly StringHash32 TimeUpdated = "save:time-updated"; // uint timeUnits
        static public readonly StringHash32 InventoryUpdated = "save:inventory-updated"; // StringHash32 storyScrapId
        static public readonly StringHash32 LocationUpdated = "save:location-updated"; // StringHash32 locationId
        static public readonly StringHash32 LevelLoading = "level:loading";
        static public readonly StringHash32 LevelStarted = "level:started";
        static public readonly StringHash32 LevelCheckpoint = "level:checkpoint";
        static public readonly StringHash32 ResumedCheckpoint = "level:resumed-checkpoint";
        static public readonly StringHash32 GameOver = "level:game-over";
        static public readonly StringHash32 GameOverClose = "level:game-over-close";
        static public readonly StringHash32 EditorNotesOpen = "ui:editor-notes-open";
        static public readonly StringHash32 EditorNotesClose = "ui:editor-notes-close";
        static public readonly StringHash32 RequireStoryPublish = "level:require-story-publish";
        static public readonly StringHash32 StoryPublished = "level:story-published";
        static public readonly StringHash32 StoryEvalBegin = "level:story-eval-begin";
        static public readonly StringHash32 StoryEvalImpact = "level:story-eval-impact";
        static public readonly StringHash32 StoryEvalEditor = "level:story-eval-editor";
        static public readonly StringHash32 StoryEvalEnd = "level:story-eval-end";
        static public readonly StringHash32 TutorialBegin = "level:tutorial-begin";
        static public readonly StringHash32 TutorialEnd = "level:tutorial-end";
        static public readonly StringHash32 RollCredits = "level:roll-credits";
        static public readonly StringHash32 ImminentFailure = "level:imminent-failure";
        static public readonly StringHash32 StatsUpdated = "save:stats-updated"; // int[] adjustments
        static public readonly StringHash32 ChoiceOptionsUpdating = "save:options-updating"; // StringHash32[] locIds
        static public readonly StringHash32 ChoiceOptionsUpdated = "save:options-updated";
        static public readonly StringHash32 ChoicesClearing = "save:choices-clearing";
        static public readonly StringHash32 ChoiceCompleted = "save:choice-completed";
        static public readonly StringHash32 PrepareTitleReturn = "title:prepare-return";
        static public readonly StringHash32 LoadTitleScreen = "title:loading";
        static public readonly StringHash32 TitleErrorReceived = "title:error";
        static public readonly StringHash32 TryNewName = "title:new-name";
        static public readonly StringHash32 TryNewGame = "title:new-game";
        static public readonly StringHash32 NewGameSuccess = "title:new-game-success";
        static public readonly StringHash32 NewNameGenerated = "title:new-name-generated";
        static public readonly StringHash32 TryContinueName = "title:continue-name";
        static public readonly StringHash32 TryContinueGame = "title:continue-game";
        static public readonly StringHash32 ContinueGameSuccess = "title:continue-game-success";
        static public readonly StringHash32 ContinueNameRetrieved = "title:continue-name-retrieved";


        static public readonly StringHash32 TextClicked = "analytics:text-click"; // TextNodeParams
        static public readonly StringHash32 DisplayTextDialog = "analytics:display-text-dialog"; // TextNodeParams
        static public readonly StringHash32 DisplayBreakdownDialog = "analytics:display-breakdown-dialog"; // StoryStats
        static public readonly StringHash32 DisplaySnippetQualityDialog = "analytics:display-snippet-quality-dialog"; // StoryStats
        static public readonly StringHash32 DisplayFeedbackDialog = "analytics:display-feedback-dialog";
        static public readonly StringHash32 DisplayChoices = "analytics:display-choices";
        static public readonly StringHash32 OpenStatsTab = "analytics:open-stats-tab";
        static public readonly StringHash32 CloseStatsTab = "analytics:close-stats-tab";
        static public readonly StringHash32 OpenMapTab = "analytics:open-map-tab";
        static public readonly StringHash32 OpenChoiceMap = "analytics:open-choice-map";
        static public readonly StringHash32 CloseMapTab = "analytics:close-map-tab";
        static public readonly StringHash32 ChangeBackgroundImage = "analytics:change-background-image";
        static public readonly StringHash32 ShowPopupImage = "analytics:show-popup-image";
        static public readonly StringHash32 UnlockedNotebook = "analytics:unlocked-notebook";
        static public readonly StringHash32 OpenNotebook = "analytics:open-notebook";
        static public readonly StringHash32 SelectSnippet = "analytics:select-snippet";
        static public readonly StringHash32 PlaceSnippet = "analytics:place-snippet";
        static public readonly StringHash32 RemoveSnippet = "analytics:remove-snippet";
        static public readonly StringHash32 CloseNotebook = "analytics:close-notebook";
        static public readonly StringHash32 TimeLimitAssigned = "analytics:time-limit-assigned";
        static public readonly StringHash32 OpenTimer = "analytics:open-timer";
        static public readonly StringHash32 CloseTimer = "analytics:close-timer";
        static public readonly StringHash32 TimeElapsed = "analytics:time-elapsed";
        static public readonly StringHash32 TimeExpired = "analytics:time-expired";
        static public readonly StringHash32 StoryUpdated = "analytics:story-updated";
        static public readonly StringHash32 DisplayPublishedStory = "analytics:display-published-story";
        static public readonly StringHash32 ClosePublishedStory = "analytics:close-published-story";
        static public readonly StringHash32 CompleteLevel = "analytics:complete-level";
        static public readonly StringHash32 StartEndgame = "analytics:start-endgame";

        static public readonly StringHash32 OnPrepareLine = "analytics:entering-node";
        static public readonly StringHash32 OnNodeStart = "analytics:starting-node";
        static public readonly StringHash32 StatsRefreshed = "analytics:stats-refreshed";
        static public readonly StringHash32 StoryImpactDisplayed = "analytics:story-impact-displayed";
        static public readonly StringHash32 SlotsLaidOut = "analytics:slots-laid-out";

        // static public readonly StringHash32 RequestSurvey = "analytics:request-survey";
        // static public readonly StringHash32 SurveyFinished = "analytics:finish-survey";
    }
}