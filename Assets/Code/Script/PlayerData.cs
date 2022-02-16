using BeauUtil;
using BeauData;
using BeauUtil.Variants;
using System.Collections.Generic;

namespace Journalism {
    public class PlayerData : ISerializedObject, ISerializedVersion {
        public int[] StatValues = new int[Stats.Count];

        public VariantTable GlobalTable = new VariantTable();
        public VariantTable UITable = new VariantTable("ui");
        public HashSet<StringHash32> VisitedNodeIds = new HashSet<StringHash32>();
        
        public uint LevelIndex = 0;
        public List<StringHash32> StoryScrapInventory = new List<StringHash32>();
        public StringHash32[] AllocatedScraps = new StringHash32[StoryScraps.MaxSlots];
        public StringHash32 CheckpointId = null;
        public StringHash32 LocationId = null;
        public uint TimeRemaining;

        #region ISerializedObject
        
        // v1: initial
        ushort ISerializedVersion.Version {
            get { return 1; }
        }

        public void Serialize(Serializer serializer) {
            serializer.Array("stats", ref StatValues);

            serializer.Object("globalVars", ref GlobalTable);
            serializer.Object("uiVars", ref UITable);
            serializer.UInt32ProxySet("visited", ref VisitedNodeIds);
            
            serializer.UInt32ProxyArray("scrapInventory", ref StoryScrapInventory);
            serializer.UInt32ProxyArray("allocatedScraps", ref AllocatedScraps);

            serializer.Serialize("levelIndex", ref LevelIndex);
            serializer.UInt32Proxy("checkpointId", ref CheckpointId);
            serializer.UInt32Proxy("locationId", ref LocationId);
            serializer.Serialize("timeRemaining", ref TimeRemaining);
        }

        #endregion // ISerializedObject
    }
}