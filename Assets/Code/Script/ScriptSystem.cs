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

            m_TextDisplay.LookupNextChoice = m_Integration.PredictChoice;
            m_TextDisplay.LookupNextLine = m_Integration.PredictNextLine;
            m_TextDisplay.LookupLine = m_Integration.LookupLine;

            Game.Events.Register(Events.LevelStarted, OnLevelStarted, this);

            DeclareData(new PlayerData());
        }

        private IEnumerator HandleNodeEnter(ScriptNode node, LeafThreadState thread) {
            if (node.HasFlags(ScriptNodeFlags.Checkpoint)) {
                Player.Data.CheckpointId = node.CheckpointId();
                Game.Save.SaveCheckpoint();
            }
            
            yield return m_TextDisplay.HandleNodeStart(node, thread);

            m_FirstVisit = Player.Data.VisitedNodeIds.Add(node.Id());
        }

        private void OnLevelStarted() {
            m_Visuals.ClearBackgrounds();
            m_TextDisplay.ClearAll();
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
        public Future LoadLevel(int levelIndex) {
            return LoadLevel(Assets.Level(levelIndex));
        }

        /// <summary>
        /// Loads level data.
        /// </summary>
        public Future LoadLevel(LevelDef level) {
            Future future = new Future();
            if (m_CurrentLevel == level) {
                future.Complete();
                return future;
            }

            if (m_CurrentLevel != null) {
                m_CurrentLevel.LoadedScript.Clear();
                m_CurrentLevel.LoadedScript = null;
                m_CurrentLevel.StoryScraps.Clear();
            }

            m_CurrentLevel = level;

            var scriptLoader = m_Integration.LoadScript(level.Script).OnComplete((s) => {
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

        #endregion // Leaf
    }
}