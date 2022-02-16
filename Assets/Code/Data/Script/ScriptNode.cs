using BeauUtil;
using BeauUtil.Blocks;
using Leaf;
using UnityEngine.Scripting;

namespace Journalism {
    public sealed class ScriptNode : LeafNode {

        private ScriptNodeFlags m_Flags;
        private StringHash32 m_CheckpointId;

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

        public StringHash32 CheckpointId() {
            return m_CheckpointId;
        }

        #region Meta Tags
        
        [BlockMeta("clearText"), Preserve]
        private void SetClearTextFlag() {
            m_Flags |= ScriptNodeFlags.ClearText;
        }

        [BlockMeta("hub"), Preserve]
        private void SetHubFlag() {
            m_Flags |= ScriptNodeFlags.Hub;
        }

        [BlockMeta("checkpoint"), Preserve]
        private void SetCheckpoint(StringHash32 id = default) {
            m_Flags |= ScriptNodeFlags.Checkpoint;
            m_CheckpointId = id.IsEmpty ? this.Id() : id;
        }

        #endregion // Meta Tags
    }

    public enum ScriptNodeFlags : ushort {
        ClearText = 0x01,
        Hub = 0x02,
        Checkpoint = 0x04
    }
}