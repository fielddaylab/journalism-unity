using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using Leaf;
using UnityEngine.Scripting;

namespace Journalism {
    public sealed class StoryScraps : ScriptableDataBlockPackage<StoryScrapData> {
        public const int MaxSlots = 6;
        
        private readonly Dictionary<StringHash32, StoryScrapData> m_Scraps = new Dictionary<StringHash32, StoryScrapData>();
        private readonly List<StorySlot> m_Slots = new List<StorySlot>();

        private string m_HeadlineType = string.Empty;

        public override int Count { get { return m_Scraps.Count; } }

        public override IEnumerator<StoryScrapData> GetEnumerator() {
            return m_Scraps.Values.GetEnumerator();
        }

        public string HeadlineType { get { return m_HeadlineType; } }
        public ListSlice<StorySlot> Slots { get { return m_Slots; } }

        public StoryScrapData Scrap(StringHash32 id) {
            StoryScrapData scrap = null;
            if (!id.IsEmpty && !m_Scraps.TryGetValue(id, out scrap)) {
                Assert.Fail("[StoryScraps] No scrap with id '{0}' found in file '{1}'", id, name);
            }
            return scrap;
        }

        public override void Clear() {
            base.Clear();

            m_Scraps.Clear();
            m_Slots.Clear();
        }

        #region Meta



        #endregion // Meta

        #region Generator

        static public readonly IBlockGenerator<StoryScrapData, StoryScraps> Parser = new Generator();

        private class Generator : GeneratorBase<StoryScraps> {
            public override bool TryCreateBlock(IBlockParserUtil inUtil, StoryScraps inPackage, TagData inId, out StoryScrapData outBlock) {
                outBlock = new StoryScrapData() {
                    Id = inId.Id
                };
                inPackage.m_Scraps.Add(outBlock.Id, outBlock);
                return true;
            }
        }

        #endregion // Generator

        #region Importer

        #if UNITY_EDITOR

        [ScriptedExtension(1, "scraps")]
        private class Importer : ImporterBase<StoryScraps> { }

        #endif // UNITY_EDITOR

        #endregion // Importer
    }
}