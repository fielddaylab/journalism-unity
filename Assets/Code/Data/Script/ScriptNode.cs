using BeauUtil;
using BeauUtil.Blocks;
using Leaf;
using UnityEngine.Scripting;

namespace Journalism {
    public sealed class ScriptNode : LeafNode {

        private string m_FullName = null;
        private ScriptNodeFlags m_Flags;
        private StringHash32 m_CheckpointId;
        private StringHash32[] m_Tags;

        public ScriptNode(string inFullId, Script inScript)
            : base(inFullId, inScript)
        {
            m_FullName = inFullId;
        }

        public string FullName() { return m_FullName; }

        public ScriptNodeFlags Flags() {
            return m_Flags;
        }

        public bool HasFlags(ScriptNodeFlags flags) {
            return (m_Flags & flags) == flags;
        }

        public StringHash32 CheckpointId() {
            return m_CheckpointId;
        }

        public bool HasTag(StringHash32 tag) {
            return m_Tags != null && ArrayUtils.Contains(m_Tags, tag);
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

        [BlockMeta("tags"), Preserve]
        private void SetCheckpoint(StringSlice data) {
            m_Tags = ArrayUtils.MapFrom(data.Split(StringUtils.ArgsList.Splitter.Instance, System.StringSplitOptions.RemoveEmptyEntries), (s) => new StringHash32(s));
        }

        #endregion // Meta Tags
    }

    public enum ScriptNodeFlags : ushort {
        ClearText = 0x01,
        Hub = 0x02,
        Checkpoint = 0x04
    }
}