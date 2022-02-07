using System.Collections;
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
        
        public delegate IEnumerator HandleNodeStartDelegate(ScriptNode node, LeafThreadState thread);

        private LeafAsset m_CurrentAsset;
        private Script m_CurrentScript;
        private LeafThreadHandle m_CurrentThread;
        private Routine m_ScriptLoader;
        private TagString m_TempTagString = new TagString();

        public LeafIntegration(MonoBehaviour inHost, CustomVariantResolver inResolver, IMethodCache inCache = null)
            : base(inHost, inResolver)
        {
        }

        public HandleNodeStartDelegate HandleNodeEnter;

        #region Loading

        public Future<Script> LoadScript(LeafAsset asset) {
            if (m_CurrentAsset == asset) {
                return Future.Completed<Script>(m_CurrentScript);
            }

            m_CurrentScript?.Clear();
            m_CurrentAsset = asset;
            m_CurrentThread.Kill();

            Future<Script> future = new Future<Script>();
            m_ScriptLoader.Replace(m_RoutineHost, LoadScriptRoutine(future));
            return future;
        }

        private IEnumerator LoadScriptRoutine(Future<Script> future) {
            m_CurrentScript = LeafAsset.CompileAsync(m_CurrentAsset, Script.Parser, out IEnumerator loader);
            AsyncHandle asyncHandle = Async.Schedule(loader, AsyncFlags.HighPriority);
            using(asyncHandle)
            using(Profiling.Time("loading script")) {
                yield return asyncHandle;
            }
            future.Complete(m_CurrentScript);
        }
    
        #endregion // Loading

        #region Starting

        public void StartFromBeginning() {
            Assert.True(m_CurrentScript != null && !m_ScriptLoader, "Cannot start while script isn't fully loaded");
            m_CurrentScript.TryGetNode(m_CurrentScript.StartNodeId(), out ScriptNode start);
            Run(start);
        }

        #endregion // Starting

        #region Run

        public override void OnNodeEnter(ScriptNode inNode, LeafThreadState<ScriptNode> inThreadState) {
            base.OnNodeEnter(inNode, inThreadState);

            IEnumerator process = HandleNodeEnter?.Invoke(inNode, inThreadState);
            if (process != null) {
                inThreadState.Interrupt(process);
            }
        }

        public override LeafThreadHandle Run(ScriptNode node, ILeafActor actor = null, VariantTable locals = null, string name = null) {
            m_CurrentThread.Kill();
            return m_CurrentThread = base.Run(node, actor, locals, name);
        }

        #endregion // Run

        #region Prediction

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

        #endregion // Prediction
    }
}