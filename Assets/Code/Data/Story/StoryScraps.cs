using System.Collections.Generic;
using BeauUtil;
using BeauUtil.Blocks;
using BeauUtil.Debugger;
using BeauUtil.Tags;
using Leaf;
using UnityEngine.Scripting;

namespace Journalism {
    public sealed class StoryScraps : ScriptableDataBlockPackage<StoryScrap> {
        private readonly Dictionary<StringHash32, StoryScrap> m_Scraps = new Dictionary<StringHash32, StoryScrap>();

        public override int Count { get { return m_Scraps.Count; } }

        public override IEnumerator<StoryScrap> GetEnumerator() {
            return m_Scraps.Values.GetEnumerator();
        }

        public StoryScrap Scrap(StringHash32 id) {
            StoryScrap scrap = null;
            if (!id.IsEmpty && !m_Scraps.TryGetValue(id, out scrap)) {
                Assert.Fail("[StoryScraps] No scrap with id '{0}' found in file '{1}'", id, name);
            }
            return scrap;
        }

        public override void Clear() {
            base.Clear();

            m_Scraps.Clear();
        }

        #region Generator

        static public readonly IBlockGenerator<StoryScrap, StoryScraps> Parser = new Generator();

        private class Generator : GeneratorBase<StoryScraps> {
            public override bool TryCreateBlock(IBlockParserUtil inUtil, StoryScraps inPackage, TagData inId, out StoryScrap outBlock) {
                outBlock = new StoryScrap() {
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