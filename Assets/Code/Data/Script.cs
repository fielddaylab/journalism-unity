using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using Leaf;
using Leaf.Compiler;
using UnityEngine;
using UnityEngine.Scripting;

namespace Journalism {
    public sealed class Script : LeafNodePackage<ScriptNode> {

        private const string FallbackStartNodeId = "START"; 

        private StringHash32 m_StartNodeId;

        public Script(string inName)
            : base(inName)
        {
        }

        [BlockMeta("start"), Preserve]
        private void SetStart(StringSlice startId) {
            if (m_RootPath.Length > 0 && startId.StartsWith('.')) {
                m_StartNodeId = m_RootPath + "." + startId;
            } else {
                m_StartNodeId = startId;
            }
        }

        public StringHash32 StartNodeId() {
            return m_StartNodeId;
        }

        #region Generator

        static public LeafParser<ScriptNode, Script> Parser {
            get { return Generator.Instance; }
        }

        private class Generator : LeafParser<ScriptNode, Script> {
            static public readonly Generator Instance = new Generator();

            public override bool IsVerbose {
                get { return Application.isEditor; }
            }

            public override Script CreatePackage(string inFileName) {
                return new Script(inFileName);
            }

            protected override ScriptNode CreateNode(string inFullId, StringSlice inExtraData, Script inPackage) {
                return new ScriptNode(inFullId, inPackage);
            }

            public override void OnEnd(IBlockParserUtil util, Script package, bool error) {
                if (package.m_StartNodeId.IsEmpty) {
                    Log.Warn("[Script] No starting node defined for '{0}': defaulting to node with local id '{1}'", package.Name(), FallbackStartNodeId);
                    package.SetStart(FallbackStartNodeId);
                }
                base.OnEnd(util, package, error);
            }
        }

        #endregion // Generator
    }
}