using UnityEngine;
using Leaf.Defaults;
using Leaf;
using Leaf.Runtime;
using System.Collections;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Variants;
using BeauRoutine;
using System;
using UnityEngine.Scripting;
using Journalism.UI;
using EasyBugReporter;
using FDLocalization;
using UnityEngine.SceneManagement;
using Journalism.Analytics;
using System.Collections.Generic;

namespace Journalism {
    public sealed class ScriptSystem : MonoBehaviour, IDumpSource {

        static private readonly StringUtils.ArgsList.Splitter s_ArgsSplitter = new StringUtils.ArgsList.Splitter();
        
        #region Inspector

        [SerializeField] private TextDisplaySystem m_TextDisplay = null;
        [SerializeField] private ScriptVisualsSystem m_Visuals = null;
        [SerializeField] private LanguageId m_DefaultLanguage = default;

        #endregion // Inspector

        private LeafIntegration m_Integration;
        private CustomVariantResolver m_Resolver;

        [NonSerialized] private LevelDef m_CurrentLevel;
        [NonSerialized] private bool m_FirstVisit;
        private LocDB m_LocDB;

        public event Action<LeafEvalContext> OnScriptError;
        public event Action OnThreadStopped;

        private void Awake() {
            m_Resolver = new CustomVariantResolver();

            BeauRoutine.Routine.Settings.ForceSingleThreaded = true;

            m_Integration = new LeafIntegration(this, m_Resolver);
            m_Integration.ConfigureDisplay(m_TextDisplay, m_TextDisplay);

            m_Integration.RuntimeConfig.OnMethodCallError += (LeafEvalContext inContext, MethodCall inMethodCall, object inTarget) => {
                OnScriptError?.Invoke(inContext);
            };
            m_Integration.RuntimeConfig.OnNodeLookupError += (LeafEvalContext inContext, StringHash32 inId, StringHash32 inLocalNodeId) => {
                OnScriptError?.Invoke(inContext);
            };
            m_Integration.HandleThreadEnd += () => {
                OnThreadStopped?.Invoke();
            };

            CustomTagParserConfig parserConfig = new CustomTagParserConfig();
            TagStringEventHandler eventHandler = new TagStringEventHandler();
            GameText.InitializeEvents(parserConfig, eventHandler, m_Integration, m_Visuals, m_TextDisplay);
            m_Integration.ConfigureTagStringHandling(parserConfig, eventHandler);

            m_Integration.HandleNodeEnter = HandleNodeEnter;
            m_Integration.HandleNodeExit = HandleNodeExit;

            m_TextDisplay.HookIntegration(m_Integration);

            m_LocDB = new LocDB(m_DefaultLanguage);
            m_LocDB.SetAsCurrentModule();
            m_LocDB.UseParser(m_Integration.Parser);

            Game.Events.Register(GameEvents.LevelStarted, OnLevelStarted, this)
                .Register(GameEvents.LevelLoading, OnLevelLoading, this);

            DeclareData(new PlayerData());
        }

        private IEnumerator HandleNodeEnter(ScriptNode node, LeafThreadState thread) {
            if (!DebugService.AutoTesting) {
                yield return m_TextDisplay.CurrentLayer.HandleNodeStart(node, thread);
            }

            if (node.HasFlags(ScriptNodeFlags.Checkpoint) && !Player.Data.VisitedNodeIds.Contains(node.Id())) {
                Player.Data.CheckpointId = node.CheckpointId();
                Game.Save.SaveCheckpoint();
            }

            m_FirstVisit = Player.Data.VisitedNodeIds.Add(node.Id());
        }

        private IEnumerator HandleNodeExit(ScriptNode node, LeafThreadState thread) {
            return null;
        }

        private void OnLevelLoading() {
            m_Integration.ConfigureDisplay(m_TextDisplay, m_TextDisplay);
        }

        private void OnLevelStarted() {
            m_Visuals.ClearBackgrounds();
            m_TextDisplay.ClearAll();
            Player.SetupLevel(m_CurrentLevel);
        }

        public void OverrideDisplay(ITextDisplayer displayer) {
            m_Integration.ConfigureDisplay(displayer, null);
        }

        public void ClearDisplayOverride() {
            m_Integration.ConfigureDisplay(m_TextDisplay, m_TextDisplay);
        }

        /// <summary>
        /// Clears all visuals.
        /// </summary>
        public IEnumerator ClearAllVisuals() {
            yield return m_TextDisplay.ClearAllAnimated();
            yield return m_Visuals.FadeOutBackgrounds();
        }

        /// <summary>
        /// Displays the story the player made.
        /// </summary>
        public IEnumerator DisplayNewspaper() {
            if (DebugService.AutoTesting) {
                yield break;
            }

            yield return m_TextDisplay.DisplayNewspaper();
        }

        #region Data

        public void DeclareData(PlayerData data) {
            m_Resolver.Clear();
            m_Resolver.SetDefaultTable(data.GlobalTable);
            m_Resolver.SetTable(data.UITable);
            Player.DeclareData(data, m_Resolver);
        }

        public void LoadLocalization(LocFileGroup group) {
            m_LocDB.BeginLoading(group.Language);
            foreach(var file in group.Files) {
                m_LocDB.LoadFile(file);
            }
            m_LocDB.EndLoading();
        }

        #endregion // Data

        /// <summary>
        /// Loads level data.
        /// </summary>
        public Future LoadLevel(int levelIndex, bool killThread = true) {
            return LoadLevel(Assets.Level(levelIndex), killThread);
        }

        /// <summary>
        /// Loads the level for the current player.
        /// </summary>
        public Future LoadLevel(PlayerData playerData) {
            int index = Math.Max(0, playerData.LevelIndex);
            return LoadLevel(index);
        }

        /// <summary>
        /// Loads level data.
        /// </summary>
        public Future LoadLevel(LevelDef level, bool killThread) {
            Future future = new Future();
            if (m_CurrentLevel == level) {
                Game.Events.Dispatch(GameEvents.LevelLoading);
                future.Complete();
                return future;
            }

            bool bUnloadScraps = m_CurrentLevel?.StoryScraps != level.StoryScraps;

            if (m_CurrentLevel != null) {
                m_CurrentLevel.LoadedScript.Clear();
                if (bUnloadScraps) {
                    m_CurrentLevel.LoadedScript = null;
                    m_CurrentLevel.StoryScraps.Clear();
                }
            }

            m_CurrentLevel = level;
            Game.Events.Dispatch(GameEvents.LevelLoading);

            var scriptLoader = m_Integration.LoadScript(level.Script, killThread).OnComplete((s) => {
                Assets.DeclareLevel(m_CurrentLevel);
                level.LoadedScript = s;
                future.Complete();
            });

            m_CurrentLevel.StoryScraps.Parse(StoryScraps.Parser);
            return future;
        }

        /// <summary>
        /// Starts from the beginning of the level.
        /// </summary>
        public void StartLevel() {
            m_Integration.StartFromBeginning();
        }

        /// <summary>
        /// Starts from the last checkpoint.
        /// </summary>
        public void StartFromCheckpoint(PlayerData data) {
            m_Integration.StartFromCheckpoint(data);
        }

        /// <summary>
        /// Starts from the last checkpoint.
        /// </summary>
        public void StartFromNode(StringHash32 nodeId) {
            m_Integration.StartFromNode(nodeId);
        }

        /// <summary>
        /// Interrupts the current script for a frame.
        /// </summary>
        public void Interrupt() {
            m_Integration.Interrupt();
        }

        /// <summary>
        /// Interrupts the current script to execute an IEnumerator.
        /// </summary>
        public void Interrupt(IEnumerator routine) {
            m_Integration.Interrupt(routine);
        }

        internal void SkipTo(StringHash32 id) {
            m_Integration.SkipTo(id);
        }

        #region Leaf

        [LeafMember("FirstVisit"), Preserve]
        static private bool LeafFirstVisit() {
            return Game.Scripting.m_FirstVisit;
        }

        [LeafMember("NextLevel"), Preserve]
        static private IEnumerator LeafNextLevel() {
            Game.Events.Dispatch(GameEvents.CompleteLevel);
            Future loader = Game.Scripting.LoadLevel(Player.Data.LevelIndex + 1, false);
            yield return Game.Scripting.ClearAllVisuals();
            yield return loader.Wait();
            Game.Scripting.StartLevel();
        }

        [LeafMember("StartEndgame"), Preserve]
        static private void LeafStartEndgame() {
            Game.Events.Dispatch(GameEvents.StartEndgame);
        }

        [LeafMember("GameOver"), Preserve]
        static private IEnumerator GameOver() {
            Game.Events.Queue(GameEvents.GameOver);
            if (DebugService.AutoTesting) {
                yield break;
            }
            
            yield return Game.Scripting.ClearAllVisuals();
            Game.Scripting.OverrideDisplay(Game.UI.GameOver);
            yield return Game.UI.GameOver.Show();
        }

        [LeafMember("GameOverRestart"), Preserve]
        static private IEnumerator GameOverChoice([BindThread] LeafThreadState thread) {
            Future<bool> choice;
            if (DebugService.AutoTesting) {
                choice = Future.Completed<bool>(true);
            } else {
                choice = Game.UI.GameOver.DisplayChoices();
                yield return choice;
            }

            thread.Kill();
            
            if (choice.Get()) {
                Routine.Start(LoadCheckpoint());
            } else {
                // TODO: Title screen? What do we do?
                Game.UI.GameOver.InstantHide();
                Game.Events?.Dispatch(GameEvents.PrepareTitleReturn);
                Game.Events?.Dispatch(GameEvents.LoadTitleScreen);
            }

            Game.Events.Dispatch(GameEvents.GameOverClose);
        }

        static private IEnumerator LoadCheckpoint() {
            Game.Save.LoadLastCheckpoint();
            var loadLevel = Game.Scripting.LoadLevel(Player.Data);
            yield return Game.UI.GameOver.Hide();
            yield return loadLevel;
            Game.Scripting.StartFromCheckpoint(Player.Data);
            Game.Events.Dispatch(GameEvents.ResumedCheckpoint, ResumedCheckpointOrigin.LevelFail);
        }

        [LeafMember("BeginFeedback"), Preserve]
        static private void BeginFeedback() {
            Game.Events.Queue(GameEvents.StoryEvalBegin);
            Player.CompileStoryStatistics();
            Player.RemoveUsedSnippets();
        }

        [LeafMember("ImpactFeedback"), Preserve]
        static private void ImpactFeedback() {
            Game.Events.Dispatch(GameEvents.StoryEvalImpact);
        }

        [LeafMember("EditorFeedback"), Preserve]
        static private void EditorFeedback() {
            Game.Events.Queue(GameEvents.StoryEvalEditor);
        }

        [LeafMember("NextFeedback"), Preserve]
        static private void NextFeedback(StringHash32 snippetId, StringHash32 locationId) {
            Game.Scripting.m_TextDisplay.EnqueueFeedbackItem(snippetId, locationId);
        }

        [LeafMember("EndFeedback"), Preserve]
        static private void EndFeedback() {
            Game.Events.Queue(GameEvents.StoryEvalEnd);
        }

        [LeafMember("BeginTutorial"), Preserve]
        static private void BeginTutorial() {
            Game.Events.Dispatch(GameEvents.TutorialBegin);
            Game.UI.PushInputMask(InputLayerFlags.OverStory);

            Game.Events.Dispatch(GameEvents.UnlockedNotebook);
        }

        [LeafMember("EndTutorial"), Preserve]
        static private void EndTutorial() {
            Game.Events.Dispatch(GameEvents.TutorialEnd);
            Game.UI.PopInputMask();
        }

        [LeafMember("MarkImminentFailure"), Preserve]
        static private void MarkImminentFailure(StringSlice failData) {
            if (failData.IsEmpty) {
                return;
            }

            TempList8<StringSlice> allTypes = default;
            failData.Split(s_ArgsSplitter, System.StringSplitOptions.RemoveEmptyEntries, ref allTypes);

            List<FailType> failTypes = new List<FailType>();

            foreach(var typeStr in allTypes)
            {
                FailType failType = StringParser.ConvertTo<FailType>(typeStr);
                failTypes.Add(failType);
            }
            
            Game.Events.Dispatch(GameEvents.ImminentFailure, failTypes);
        }

        [LeafMember("HasChoices"), Preserve]
        static private bool HasChoices([BindThread] LeafThreadState thread) {
            return thread.AvailableOptionCount(ScriptSystem.ChoicePredicate) > 0;
        }

        #endregion // Leaf

        static public readonly LeafChoice.OptionPredicate ChoicePredicate = (choice, option) => {
            float timeCost = Mathf.Max(0, choice.GetCustomData(option.Index, GameText.ChoiceData.Time).AsFloat());
            if (!Player.HasTime(timeCost)) {
                return false;
            }

            bool once = choice.HasCustomData(option.Index, GameText.ChoiceData.Once);
            if (once && Player.Visited(option.TargetId.AsStringHash())) {
                return false;
            }

            return true;
        };

        bool IDumpSource.Dump(IDumpWriter writer) {
            writer.Header("Leaf State");
            writer.KeyValue("Current Level", m_CurrentLevel?.name);
            writer.KeyValue("Current Node", m_Integration.PeekNode()?.Id().ToDebugString());
            return true;
        }
    }
}