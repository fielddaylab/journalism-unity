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

namespace Journalism {
    public sealed class ScriptSystem : MonoBehaviour {

        #region Inspector

        [SerializeField] private TextDisplaySystem m_TextDisplay = null;
        [SerializeField] private ScriptVisualsSystem m_Visuals = null;

        #endregion // Inspector

        private LeafIntegration m_Integration;
        private CustomVariantResolver m_Resolver;

        [NonSerialized] private LevelDef m_CurrentLevel;
        [NonSerialized] private bool m_FirstVisit;

        private void Awake() {
            m_Resolver = new CustomVariantResolver();

            BeauRoutine.Routine.Settings.ForceSingleThreaded = true;

            m_Integration = new LeafIntegration(this, m_Resolver);
            m_Integration.ConfigureDisplay(m_TextDisplay, m_TextDisplay);

            CustomTagParserConfig parserConfig = new CustomTagParserConfig();
            TagStringEventHandler eventHandler = new TagStringEventHandler();
            GameText.InitializeEvents(parserConfig, eventHandler, m_Integration, m_Visuals, m_TextDisplay);
            m_Integration.ConfigureTagStringHandling(parserConfig, eventHandler);

            m_Integration.HandleNodeEnter = HandleNodeEnter;
            m_Integration.HandleNodeExit = HandleNodeExit;

            m_TextDisplay.LookupNextChoice = m_Integration.PredictChoice;
            m_TextDisplay.LookupNextLine = m_Integration.PredictNextLine;
            m_TextDisplay.LookupLine = m_Integration.LookupLine;

            Game.Events.Register(GameEvents.LevelStarted, OnLevelStarted, this)
                .Register(GameEvents.LevelLoading, OnLevelLoading, this);

            DeclareData(new PlayerData());
        }

        private IEnumerator HandleNodeEnter(ScriptNode node, LeafThreadState thread) {
            if (node.HasFlags(ScriptNodeFlags.Checkpoint)) {
                Player.Data.CheckpointId = node.CheckpointId();
                Game.Save.SaveCheckpoint();
            }

            if (node.HasFlags(ScriptNodeFlags.Feedback)) {
                Game.Events.Queue(GameEvents.StoryEvalBegin);
                Player.CompileStoryStatistics();
            }
            
            yield return m_TextDisplay.HandleNodeStart(node, thread);

            m_FirstVisit = Player.Data.VisitedNodeIds.Add(node.Id());
        }

        private IEnumerator HandleNodeExit(ScriptNode node, LeafThreadState thread) {
            if (node.HasFlags(ScriptNodeFlags.Feedback)) {
                Game.Events.Queue(GameEvents.StoryEvalEnd);
            }

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
            yield return m_TextDisplay.DisplayNewspaper();
        }

        #region Data

        public void DeclareData(PlayerData data) {
            m_Resolver.Clear();
            m_Resolver.SetDefaultTable(data.GlobalTable);
            m_Resolver.SetTable(data.UITable);
            Player.DeclareData(data, m_Resolver);
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

        #region Leaf

        [LeafMember("FirstVisit"), Preserve]
        static private bool LeafFirstVisit() {
            return Game.Scripting.m_FirstVisit;
        }

        [LeafMember("NextLevel"), Preserve]
        static private IEnumerator LeafNextLevel() {
            Future loader = Game.Scripting.LoadLevel(Player.Data.LevelIndex + 1, false);
            yield return Game.Scripting.ClearAllVisuals();
            yield return loader.Wait();
            Game.Scripting.StartLevel();
        }

        [LeafMember("GameOver"), Preserve]
        static private IEnumerator GameOver() {
            Game.Events.Queue(GameEvents.GameOver);
            yield return Game.Scripting.ClearAllVisuals();
            Game.Scripting.OverrideDisplay(Game.UI.GameOver);
            yield return Game.UI.GameOver.Show();
        }

        [LeafMember("GameOverRestart")]
        static private IEnumerator GameOverChoice([BindThread] LeafThreadState thread) {
            Future<bool> choice = Game.UI.GameOver.DisplayChoices();
            yield return choice;

            thread.Kill();
            
            if (choice.Get()) {
                Routine.Start(LoadCheckpoint());
            } else {
                // TODO: Title screen? What do we do?
                yield return Game.UI.GameOver.Hide();
            }

            Game.Events.Dispatch(GameEvents.GameOverClose);
        }

        static private IEnumerator LoadCheckpoint() {
            Game.Save.LoadLastCheckpoint();
            var loadLevel = Game.Scripting.LoadLevel(Player.Data);
            yield return Game.UI.GameOver.Hide();
            yield return loadLevel;
            Game.Scripting.StartFromCheckpoint(Player.Data);
        }

        #endregion // Leaf
    }
}