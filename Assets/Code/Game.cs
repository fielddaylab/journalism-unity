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
            Assets.DeclareLevelList(m_LevelDefs);
            Stats.Import(m_Stats);
            MapLocations.Import(m_MapLocationDef);

            Streaming.TextureMemoryBudget = 16 * 1024 * 1024;
            Streaming.AudioMemoryBudget = 16 * 1024 * 1024;

            Game.Events.Register<string>(GameEvents.TryContinueGame, OnTryContinueGame, this)
                .Register(GameEvents.TryNewGame, OnTryNewGame, this);
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

        private void OnTryNewGame() {

            // TODO: Pause input

            Debug.Log("new game attempted...");
            OGD.Player.NewId(OnNewNameSuccess, OnNewNameFail);
        }

        private void OnNewNameSuccess(string inName) {
            Debug.Log("new name succeeded...");

            Game.Events.Dispatch(GameEvents.NewNameGenerated, inName);

            Routine.Start(this, NewGame(inName));
        }

        private IEnumerator NewGame(string inName) {

            Future<bool> newSave = m_SaveSystem.NewServerSave(inName);
            yield return newSave;

            if (!newSave.IsComplete()) {
                Debug.Log("Save Error");
                // TODO: Resume Input
            }
            else {
                Debug.Log("Successful Name: " + inName);
                //m_SaveSystem.NewLocalSave();

                m_ScriptSystem.LoadLevel(0).OnComplete(() => {
                    m_ScriptSystem.StartLevel();

                    // TODO: Resume Input
                });
            }

        }

        private void OnNewNameFail(OGD.Core.Error error) {
            Log.Error("[Game] Generating new player id failed: {0}", error.Msg);
            //Services.Input.ResumeAll();
        }

        #endregion // New Game

        #region Continue Game

        private void OnTryContinueGame(string userCode) {
            // TODO: Pause Input

            m_SaveSystem.ReadServerSave(userCode)
                .OnComplete(OnContinueGameSuccess)
                .OnFail(OnContinueGameFail);
        }

        private void OnContinueGameSuccess(PlayerData data) {
            m_ScriptSystem.LoadLevel(Player.Data.LevelIndex).OnComplete(() => {
                m_ScriptSystem.StartLevel();

                // TODO: Resume Input
            });
        }

        private void OnContinueGameFail() {
            Debug.Log("Continue failed");

            // TODO: Resume Input
        }

        #endregion // Continue Game
    }
}