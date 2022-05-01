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

namespace Journalism.UI {
    public sealed class ImpactLayout : MonoBehaviour, ITextDisplayer {

        public struct Item {
            public string RichText;
            public StringHash32 Location;
            public StringHash32 Style;
        }

        public RectTransform Root;
        public CanvasGroup Group;
        public ImpactFeedbackPin[] Pins;
        public SerializedHash32 DefaultStyle;


        public RingBuffer<Item> Items = new RingBuffer<Item>();

        public void Enqueue(StringHash32 location, StringHash32 style) {

        }

        #region ITextDisplayer

        IEnumerator ITextDisplayer.CompleteLine() {
            Game.Scripting.ClearDisplayOverride();
            return null;
        }

        TagStringEventHandler ITextDisplayer.PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            // if (inString.RichText.Length > 0) {
            //     ref Item item = 
            //     item.RichText = inString.RichText;
            // }

            return null;
        }

        IEnumerator ITextDisplayer.TypeLine(TagString inSourceString, TagTextData inType) {
            return null;
        }

        #endregion // ITextDisplayer
    }
}