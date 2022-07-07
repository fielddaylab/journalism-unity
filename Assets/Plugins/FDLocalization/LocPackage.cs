using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using BeauUtil;
using BeauUtil.Tags;
using BeauUtil.Blocks;
using UnityEngine;
using UnityEngine.Scripting;
using System.Collections;

namespace FDLocalization
{
    /// <summary>
    /// Localization file.
    /// </summary>
    public sealed class LocPackage : IDataBlockPackage<LocPackage.Node> {
        private Dictionary<uint, string> m_Strings = new Dictionary<uint, string>();
        private HashSet<uint> m_StringsWithTags = new HashSet<uint>();
        [NonSerialized] private string m_Name;
        [BlockMeta("basePath"), Preserve] private string m_RootPath = string.Empty;
        [NonSerialized] private Node m_CachedNode;

        public LocPackage(string fileName) {
            m_Name = fileName;
        }

        /// <summary>
        /// Clears this package.
        /// </summary>
        public void Clear() {
            m_RootPath = string.Empty;
            m_Strings.Clear();
            m_StringsWithTags.Clear();
        }

        #region Access

        /// <summary>
        /// Attempts to retrive the string associated with the given 
        /// </summary>
        [MethodImpl(256)]
        public bool TryGet(LocId id, out string content) {
            return m_Strings.TryGetValue(id.Hash().HashValue, out content);
        }

        [MethodImpl(256)]
        public string Get(LocId id, string defaultContent = null) {
            string content;
            if (!m_Strings.TryGetValue(id.Hash().HashValue, out content)) {
                content = defaultContent;
            }
            return content;
        }

        public string this[LocId id] {
            [MethodImpl(256)] get { return Get(id); }
        }

        [MethodImpl(256)]
        public bool HasPotentialTags(LocId id) {
            return m_StringsWithTags.Contains(id.Hash().HashValue);
        }

        #endregion // Access

        #region Overrides

        public int Count { get { return m_Strings.Count; } }

        public IEnumerator<Node> GetEnumerator() {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotSupportedException();
        }

        #endregion // Overrides

        public sealed class Node : IDataBlock {
            public StringHash32 Id;
            [BlockContent] public string Content;
            
            [BlockMeta("exposed"), Preserve] private void Unused() { }
        }

        #region Generator

        static public readonly IBlockGenerator<Node, LocPackage> Generator = new GeneratorImpl();

        private class GeneratorImpl : AbstractBlockGenerator<Node, LocPackage> {
            public override LocPackage CreatePackage(string inFileName) {
                return new LocPackage(inFileName);
            }

            public override bool TryCreateBlock(IBlockParserUtil inUtil, LocPackage inPackage, TagData inId, out Node outBlock) {
                StringSlice id = inId.Id;
                StringSlice root = inPackage.m_RootPath;
                StringHash32 hash;
                if (id.StartsWith('.')) {
                    id = id.Substring(1);
                }

                if (!root.IsEmpty) {
                    if (root.EndsWith('.')) {
                        root = root.Substring(0, root.Length - 1);
                    }

                    inUtil.TempBuilder.Length = 0;
                    inUtil.TempBuilder.AppendSlice(root)
                        .Append('.').AppendSlice(id);

                    StringBuilderSlice slice = new StringBuilderSlice(inUtil.TempBuilder);
                    hash = slice.Hash32();
                    inUtil.TempBuilder.Length = 0;
                } else {
                    hash = id.Hash32();
                }

                Node block = inPackage.m_CachedNode ?? (inPackage.m_CachedNode = new Node());
                block.Id = hash;
                block.Content = string.Empty;
                outBlock = block;
                return true;
            }

            public override void CompleteBlock(IBlockParserUtil inUtil, LocPackage inPackage, Node inBlock, bool inbError) {
                inPackage.m_Strings.Add(inBlock.Id.HashValue, inBlock.Content);
                if (inBlock.Content.IndexOf('{') >= 0) {
                    inPackage.m_StringsWithTags.Add(inBlock.Id.HashValue);
                }
                inBlock.Content = string.Empty;
                inBlock.Id = default;
            }
        }

        #endregion // Generator
    }
}