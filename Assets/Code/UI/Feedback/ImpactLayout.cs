using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;
using Leaf;
using Leaf.Defaults;
using System.Collections;
using BeauUtil.Tags;
using BeauPools;

namespace Journalism.UI {
    public sealed class ImpactLayout : MonoBehaviour, ITextDisplayer {

        public struct Item {
            public string RichText;
            public StringHash32 SnippetId;
            public StringHash32 Location;
            public StringHash32 Style;
            public bool Locked;
        }

        public RectTransform Root;
        public CanvasGroup Group;
        public ImpactFeedbackPin[] Pins;
        public SerializedHash32 DefaultStyle;
        public float PinLineOffset = 16;

        public readonly RingBuffer<Item> Items = new RingBuffer<Item>();
        public readonly HashSet<StringHash32> Locations = new HashSet<StringHash32>();

        public void Clear() {
            Items.Clear();
            Locations.Clear();
        }

        public void Enqueue(StringHash32 snippetId, StringHash32 location) {
            Item item;
            item.SnippetId = snippetId;
            item.Location = location;
            item.RichText = string.Empty;
            item.Location = location;
            item.Style = DefaultStyle;
            item.Locked = false;
            Items.PushBack(item);
            Locations.Add(location);
        }

        #region ITextDisplayer

        IEnumerator ITextDisplayer.CompleteLine() {
            Game.Scripting.ClearDisplayOverride();
            return null;
        }

        TagStringEventHandler ITextDisplayer.PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            if (inString.RichText.Length > 0) {
                ref Item item = ref Items[Items.Count - 1];
                if (!LeafUtils.TryFindCharacterId(inString, out item.Style)) {
                    item.Style = DefaultStyle;
                }
                item.RichText = inString.RichText;
            }

            return null;
        }

        IEnumerator ITextDisplayer.TypeLine(TagString inSourceString, TagTextData inType) {
            return null;
        }

        #endregion // ITextDisplayer
    }
}