using System;
using System.Collections;
using System.Collections.Generic;
using BeauRoutine;
using BeauUtil;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using BeauUtil.Variants;
using Leaf;
using Leaf.Defaults;
using Leaf.Runtime;
using UnityEngine;

namespace Journalism {
    public sealed class LeafIntegration : DefaultLeafManager<ScriptNode> {
        
        public delegate IEnumerator HandleNodeDelegate(ScriptNode node, LeafThreadState thread);

        private LeafAsset m_CurrentAsset;
        private Script m_CurrentScript;
        private LeafThreadHandle m_CurrentThread;
        private Routine m_ScriptLoader;
        private TagString m_TempTagString = new TagString();
        private StringHash32 m_LastKnownNodeId;

        public LeafIntegration(MonoBehaviour inHost, CustomVariantResolver inResolver, IMethodCache inCache = null)
            : base(inHost, inResolver)
        {
            m_MethodCache.LoadStatic();
        }

        public HandleNodeDelegate HandleNodeEnter;
        public HandleNodeDelegate HandleNodeExit;
        public Action HandleThreadEnd;

        #region Loading

        public Future<Script> LoadScript(LeafAsset asset, bool killThread) {
            if (m_CurrentAsset == asset) {
                return Future.Completed<Script>(m_CurrentScript);
            }

            m_CurrentScript?.Clear();
            m_CurrentAsset = asset;
            if (killThread) {
                m_CurrentThread.Kill();
            }

            Future<Script> future = new Future<Script>();
            m_ScriptLoader.Replace(m_RoutineHost, LoadScriptRoutine(future));
            return future;
        }

        private IEnumerator LoadScriptRoutine(Future<Script> future) {
            m_CurrentScript = LeafAsset.CompileAsync(m_CurrentAsset, Script.Parser, out IEnumerator loader);
            AsyncHandle asyncHandle = Async.Schedule(loader, AsyncFlags.HighPriority);
            using(asyncHandle) {
                using(Profiling.Time("loading script")) {
                    yield return asyncHandle;
                }
                future.Complete(m_CurrentScript);
            }
        }
    
        #endregion // Loading

        #region Interrupts

        /// <summary>
        /// Interrupts the given thread.
        /// </summary>
        public void Interrupt(IEnumerator routine) {
            m_CurrentThread.GetThread()?.Interrupt(routine);
        }

        /// <summary>
        /// Interrupts the given thread.
        /// </summary>
        public void Interrupt() {
            m_CurrentThread.GetThread()?.Interrupt();
        }

        #endregion // Interrupts

        #region Starting

        public void StartFromBeginning() {
            Assert.True(m_CurrentScript != null && !m_ScriptLoader, "Cannot start while script isn't fully loaded (current script {0})", m_CurrentScript);
            m_CurrentScript.TryGetNode(m_CurrentScript.StartNodeId(), out ScriptNode start);
            Game.Events.Dispatch(GameEvents.LevelStarted);
            Run(start);
        }

        public void StartFromCheckpoint(PlayerData data) {
            if (data.CheckpointId.IsEmpty) {
                StartFromBeginning();
                return;
            }
            
            Assert.True(m_CurrentScript != null && !m_ScriptLoader, "Cannot start while script isn't fully loaded");
            m_CurrentScript.TryGetNode(data.CheckpointId, out ScriptNode start);
            Game.Events.Dispatch(GameEvents.LevelStarted);
            Run(start);
        }

        public void StartFromNode(StringHash32 nodeId) {
            Assert.True(m_CurrentScript != null && !m_ScriptLoader, "Cannot start while script isn't fully loaded");
            m_CurrentScript.TryGetNode(nodeId, out ScriptNode start);
            Game.Events.Dispatch(GameEvents.LevelStarted);
            Run(start);
        }

        public void SkipTo(StringHash32 nodeId) {
            var threadState = m_CurrentThread.GetThread<LeafThreadState<ScriptNode>>();
            LeafRuntime.TryGotoNode(this, threadState, threadState.PeekNode(), nodeId);
        }

        #endregion // Starting

        #region Run

        public override void OnNodeEnter(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState) {
            base.OnNodeEnter(inNode, inThreadState);

            if (m_CurrentThread != inThreadState.GetHandle()) {
                m_CurrentThread = inThreadState.GetHandle();
                inThreadState.Interrupt();
            }

            m_LastKnownNodeId = inNode.Id();
            Log.Msg("[LeafIntegration] Entering node {0}...", inNode.Id());

            IEnumerator process = HandleNodeEnter?.Invoke(inNode, inThreadState);
            if (process != null) {
                inThreadState.Interrupt(process);
            }
        }

        public override void OnNodeExit(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState) {
            base.OnNodeExit(inNode, inThreadState);

            IEnumerator process = HandleNodeExit?.Invoke(inNode, inThreadState);
            if (process != null) {
                inThreadState.Interrupt(process);
            }
        }

        public override void OnEnd(LeafThreadState<ScriptNode> inThreadState) {
            base.OnEnd(inThreadState);
            if (!m_LastKnownNodeId.ToDebugString().EndsWith("Feedback")) {
                Log.Warn("Thread finished in script {0}, node {1}", m_CurrentScript.Name(), m_LastKnownNodeId);
            }
            HandleThreadEnd?.Invoke();
        }

        public override LeafThreadHandle Run(ScriptNode node, ILeafActor actor = null, VariantTable locals = null, string name = null, bool _ = true) {
            m_CurrentThread.Kill();
            return m_CurrentThread = base.Run(node, actor, locals, name, _);
        }

        public override IEnumerator ShowOptions(LeafThreadState<ScriptNode> inThreadState, LeafChoice inChoice) {
            if (DebugService.AutoTesting) {
                List<LeafChoice.Option> options = new List<LeafChoice.Option>(inChoice.AvailableOptions(ScriptSystem.ChoicePredicate));
                if (options.Count > 6) {
                    options.RemoveRange(6, options.Count - 6);
                }
                if (options.Count == 0) {
                    Log.Error("No options remaining at {0} (time remaining {1})", inThreadState.PeekNode().Id(), Player.TimeRemaining());
                } else {
                    inChoice.Choose(RNG.Instance.Next(0, options.Count));
                }
            } else {
                yield return base.ShowOptions(inThreadState, inChoice);
            }
            float chosenTime = inChoice.GetCustomData(inChoice.ChosenIndex(), GameText.ChoiceData.Time).AsFloat();
            if (chosenTime > 0) {
                Player.DecreaseTime(chosenTime);
            }
        }

        #if UNITY_EDITOR

        public override IEnumerator RunLine(LeafThreadState<ScriptNode> inThreadState, StringSlice inLine) {
            #if UNITY_EDITOR
            if (DebugService.AutoTesting) {
                return null;
            }
            #endif // UNITY_EDITOR

            return base.RunLine(inThreadState, inLine);
        }

        #endif // UNITY_EDITOR)

        #endregion // Run

        #region Prediction

        public ScriptNode PeekNode() {
            var thread = m_CurrentThread.GetThread<LeafThreadState<ScriptNode>>();
            return thread?.PeekNode();
        }

        public bool PredictChoice() {
            return LeafRuntime.PredictChoice(m_CurrentThread.GetThread());
        }

        public TagString PredictNextLine() {
            m_TempTagString.Clear();

            var thread = m_CurrentThread.GetThread<LeafThreadState<ScriptNode>>();
            StringHash32 lineCode = LeafRuntime.PredictLine(thread);
            if (lineCode.IsEmpty) {
                return null;
            }

            if (LeafUtils.TryLookupLine(this, lineCode, thread.PeekNode(), out string line)) {
                m_TagParser.Parse(ref m_TempTagString, line, LeafEvalContext.FromThreadHandle(m_CurrentThread));
                return m_TempTagString;
            }

            return null;
        }

        public TagString LookupLine(StringHash32 id) {
            var thread = m_CurrentThread.GetThread<LeafThreadState<ScriptNode>>();
            
            if (LeafUtils.TryLookupLine(this, id, thread.PeekNode(), out string line)) {
                m_TagParser.Parse(ref m_TempTagString, line, LeafEvalContext.FromThreadHandle(m_CurrentThread));
                return m_TempTagString;
            }

            return null;
        }

        #endregion // Prediction
    }
}