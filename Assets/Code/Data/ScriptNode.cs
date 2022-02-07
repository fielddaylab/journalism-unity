using BeauUtil;
using BeauUtil.Blocks;
using Leaf;

namespace Journalism {
    public sealed class ScriptNode : LeafNode {

        private ScriptNodeFlags m_Flags;

        public ScriptNode(StringHash32 inId, Script inScript)
            : base(inId, inScript)
        {
        }

        public ScriptNodeFlags Flags() {
            return m_Flags;
        }

        public bool HasFlags(ScriptNodeFlags flags) {
            return (m_Flags & flags) == flags;
        }

        #region Meta Tags
        
        [BlockMeta("clearText")]
        private void SetClearTextFlag() {
            m_Flags |= ScriptNodeFlags.ClearText;
        }

        #endregion // Meta Tags
    }

    public enum ScriptNodeFlags : ushort {
        ClearText = 0x01
    }
}