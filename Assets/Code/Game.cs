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

namespace Journalism {

    [DefaultExecutionOrder(-10000)]
    public sealed class Game : Singleton<Game> {
        #region Inspector

        [Header("Assets")]
        [SerializeField] private TextStyles m_Styles = null;
        [SerializeField] private StatsDef m_Stats = null;
        [SerializeField] private LevelDef[] m_LevelDefs = null;
        
        [Header("Controllers")]
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
        }

        private void Start() {
            m_ScriptSystem.LoadLevel(0).OnComplete(() => {
                m_ScriptSystem.StartLevel();
            });
        }

        private void LateUpdate() {
            m_EventDispatcher.ProcessAsync();
        }

        /// <summary>
        /// Global game event dispatcher.
        /// </summary>
        static public EventDispatcher<object> Events {
            get { return Game.I?.m_EventDispatcher; }
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
    }
}