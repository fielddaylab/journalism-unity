using BeauUtil;

namespace Journalism {
    static public class GameEvents {
        static public readonly StringHash32 VariableUpdated = "save:variable-updated"; // TableKeyPair variableId
        static public readonly StringHash32 SaveDeclared = "save:declared"; // PlayerData data
        static public readonly StringHash32 TimeUpdated = "save:time-updated"; // uint timeUnits
        static public readonly StringHash32 InventoryUpdated = "save:inventory-updated"; // StringHash32 storyScrapId
        static public readonly StringHash32 Locationupdated = "save:location-updated"; // StringHash32 locationId
        static public readonly StringHash32 LevelLoading = "level:loading";
        static public readonly StringHash32 LevelStarted = "level:started";
        static public readonly StringHash32 LevelCheckpoint = "level:checkpoint";
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
        static public readonly StringHash32 StatsUpdated = "save:stats-updated"; // int[] adjustments
        static public readonly StringHash32 ChoiceOptionsUpdating = "save:options-updating"; // StringHash32[] locIds
        static public readonly StringHash32 ChoiceOptionsUpdated = "save:options-updated";
        static public readonly StringHash32 ChoicesClearing = "save:choices-clearing";
        static public readonly StringHash32 ChoiceCompleted = "save:choice-completed";
        static public readonly StringHash32 LoadTitleScreen = "title:loading";
        static public readonly StringHash32 TryNewName = "title:new-name";
        static public readonly StringHash32 TryNewGame = "title:new-game";
        static public readonly StringHash32 NewNameGenerated = "title:new-name-generated";
        static public readonly StringHash32 TryContinueName = "title:continue-name";
        static public readonly StringHash32 TryContinueGame = "title:continue-game";
        static public readonly StringHash32 ContinueNameRetrieved = "title:continue-name-retrieved";

    }
}