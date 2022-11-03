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
using BeauUtil.Extensions;
using Journalism.UI;
using EasyAssetStreaming;
using FDLocalization;

namespace Journalism {

    [DefaultExecutionOrder(-10000)]
    public sealed class Game : Singleton<Game> {
        #region Inspector

        [Header("Assets")]
        [SerializeField] private TextStyles m_Styles = null;
        [SerializeField] private TextChars m_Chars = null;
        [SerializeField] private StatsDef m_Stats = null;
        [SerializeField] private MapLocationDef m_MapLocationDef;
        [SerializeField] private LevelDef[] m_LevelDefs = null;
        [SerializeField] private LocFileGroup m_DefaultLoc = null;
        
        [Header("Controllers")]
        [SerializeField] private AudioSystem m_AudioSystem = null;
        [SerializeField] private ScriptSystem m_ScriptSystem = null;
        [SerializeField] private UISystem m_UISystem = null;
        [SerializeField] private SaveSystem m_SaveSystem = null;

        #endregion // Inspector

        private readonly EventDispatcher<object> m_EventDispatcher = new EventDispatcher<object>();

        protected override void Awake() {
            base.Awake();

            Assets.DeclareStyles(m_Styles);
            Assets.DeclareChars(m_Chars);
            Assets.DeclareLevelList(m_LevelDefs);
            Stats.Import(m_Stats);
            MapLocations.Import(m_MapLocationDef);

            Streaming.TextureMemoryBudget = 16 * 1024 * 1024;
            Streaming.AudioMemoryBudget = 16 * 1024 * 1024;

            Game.Events.Register(GameEvents.TryContinueName, OnTryContinueName, this)
                .Register<string>(GameEvents.TryContinueGame, OnTryContinueGame, this)
                .Register(GameEvents.TryNewName, OnTryNewName, this)
                .Register<string>(GameEvents.TryNewGame, OnTryNewGame, this);
        }

        private void Start() {
            m_ScriptSystem.LoadLocalization(m_DefaultLoc);

            Game.Events.Dispatch(GameEvents.LoadTitleScreen);
        }

        private void LateUpdate() {
            m_EventDispatcher.FlushQueue();
        }

        /// <summary>
        /// Global game event dispatcher.
        /// </summary>
        static public EventDispatcher<object> Events {
            get { return Game.I?.m_EventDispatcher; }
        }

        /// <summary>
        /// Audio system.
        /// </summary>
        static public AudioSystem Audio {
            get { return Game.I?.m_AudioSystem; }
        }

        /// <summary>
        /// Scripting system.
        /// </summary>
        static public ScriptSystem Scripting {
            get { return Game.I?.m_ScriptSystem; }
        }

        /// <summary>
        /// UI system.
        /// </summary>
        static public UISystem UI {
            get { return Game.I?.m_UISystem; }
        }

        /// <summary>
        /// Save system.
        /// </summary>
        static public SaveSystem Save {
            get { return Game.I?.m_SaveSystem; }
        }

        #region New Game

        private void OnTryNewName() {
            // Pause input
            Game.UI.PushInputMask(InputLayerFlags.Menus);

            OGD.Player.NewId(OnNewNameSuccess, OnNewNameFail);
        }

        private void OnNewNameSuccess(string inName) {
            Game.Events.Dispatch(GameEvents.NewNameGenerated, inName);
        }

        private void OnNewNameFail(OGD.Core.Error error) {
            Log.Error("[Game] Generating new player id failed: {0}", error.Msg);

            // Resume Input
            Game.UI.PushInputMask(InputLayerFlags.OverStory);
        }

        private void OnTryNewGame(string inName) {
            Routine.Start(this, NewGame(inName));
        }

        private IEnumerator NewGame(string inName) {

            Future<bool> newSave = m_SaveSystem.NewServerSave(inName);
            yield return newSave;

            if (!newSave.IsComplete()) {
                // Resume Input
                Game.UI.PushInputMask(InputLayerFlags.OverStory);
            }
            else {
                m_ScriptSystem.LoadLevel(0).OnComplete(() => {
                    m_ScriptSystem.StartLevel();
                });
            }

        }

        #endregion // New Game

        #region Continue Game

        private void OnTryContinueName() {
            // Pause Input
            Game.UI.PushInputMask(InputLayerFlags.Menus);

            string lastKnownName = m_SaveSystem.LastProfileName();

            Game.Events.Dispatch(GameEvents.ContinueNameRetrieved, lastKnownName);
        }

        private void OnContinueGameSuccess(PlayerData data) {
            int levelIndex = Player.Data.LevelIndex;
            if (levelIndex < 0) {
                Log.Warn("Previous player level data was not saved correctly. Should be a non-negative value. Starting at Level 1.");
                levelIndex = 0;
            }
            m_ScriptSystem.LoadLevel(levelIndex).OnComplete(() => {
                m_ScriptSystem.StartFromCheckpoint(data);
            });
        }

        private void OnContinueGameFail() {
            // Resume Input
            Game.UI.PushInputMask(InputLayerFlags.OverStory);
        }

        private void OnTryContinueGame(string userCode) {
            // TODO: Pause Input
            Game.UI.PushInputMask(InputLayerFlags.Menus);

            m_SaveSystem.ReadServerSave(userCode)
                .OnComplete(OnContinueGameSuccess)
                .OnFail(OnContinueGameFail);
        }

        #endregion // Continue Game
    }
}