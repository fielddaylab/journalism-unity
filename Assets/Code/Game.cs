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

namespace Journalism {

    [DefaultExecutionOrder(-10000)]
    public sealed class Game : Singleton<Game> {
        [SerializeField] private StatsDef m_Stats = null;

        private readonly EventDispatcher<object> m_EventDispatcher = new EventDispatcher<object>();

        protected override void Awake() {
            base.Awake();

            Stats.Import(m_Stats);
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
    }
}