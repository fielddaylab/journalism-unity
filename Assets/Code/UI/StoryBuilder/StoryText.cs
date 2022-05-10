using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;
using BeauPools;
using System.Collections;

namespace Journalism.UI {
    static public class StoryText {
        static public void FillSlot(StoryBuilderSlot slot, StoryScrapData data) {
            slot.Snippet.gameObject.SetActive(true);
            slot.RemoveButton.gameObject.SetActive(true);
            slot.EmptyGroup.gameObject.SetActive(false);
            slot.Data = data;
            slot.Animation.Stop();
            GameText.PopulateStoryScrap(slot.Snippet, data, Assets.Style("snippet-in-story"));
        }

        static public void EmptySlot(StoryBuilderSlot slot) {
            slot.Snippet.gameObject.SetActive(false);
            slot.RemoveButton.gameObject.SetActive(false);
            slot.EmptyGroup.gameObject.SetActive(true);
            slot.Animation.Stop();
            slot.Data = null;
        }

        static public void SetSlotType(StoryBuilderSlot slot, StorySlot data, int index) {
            slot.Index = index;

            switch(data.Type) {
                case StorySlotType.Picture: {
                    slot.EmptyIcon.gameObject.SetActive(true);
                    slot.EmptyLabel.text = "Picture Lead"; // TODO: Real text?
                    slot.Filter = StoryScrapType.ImageMask;
                    break;
                }

                default: {
                    slot.EmptyIcon.gameObject.SetActive(false);
                    slot.EmptyLabel.text = "Snippet";
                    slot.Filter = StoryScrapType.AnyMask;
                    break;
                }
            }
        }

        static public void LayoutSlots(StorySlotLayout layout, StoryConfig configuration) {
            int layoutIdx = 0;
            StoryBuilderSlot layoutSlot;
            float layoutWidth;
            layout.ActiveSlots.Clear();
            for(int dataIdx = 0; dataIdx < configuration.Slots.Length; dataIdx++) {
                var slotData = configuration.Slots[dataIdx];
                bool isLeft = (layoutIdx % 2) == 0;
                if (slotData.Wide) {
                    if (!isLeft) {
                        layout.Slots[layoutIdx].gameObject.SetActive(false);
                        layoutIdx++;
                    }
                    layoutSlot = layout.Slots[layoutIdx];
                    layout.Slots[layoutIdx + 1].gameObject.SetActive(false);
                    layoutIdx += 2;
                    layoutWidth = layout.SlotWidth * 2 + layout.SlotSpacing;
                } else {
                    layoutSlot = layout.Slots[layoutIdx];
                    layoutIdx++;
                    layoutWidth = layout.SlotWidth;
                }

                layoutSlot.gameObject.SetActive(true);
                layoutSlot.Size.SetSizeDelta(layoutWidth, Axis.X);
                SetSlotType(layoutSlot, slotData, dataIdx);
                layoutSlot.HoverHighlight.gameObject.SetActive(false);
                layoutSlot.AvailableHighlight.gameObject.SetActive(false);
                layoutSlot.Flash.gameObject.SetActive(false);
                layout.ActiveSlots.Add(layoutSlot);
            }

            for(; layoutIdx < layout.Slots.Length; layoutIdx++) {
                layout.Slots[layoutIdx].gameObject.SetActive(false);
            }

            layout.StoryType.SetText(configuration.HeadlineType);
        }

        static public void LayoutNewspaper(NewspaperLayout layout, StoryConfig config, PlayerData data) {
            int layoutIdx = 0;
            StoryScrapDisplay layoutSlot;
            float layoutWidth;
            for(int dataIdx = 0; dataIdx < config.Slots.Length; dataIdx++) {
                var slotData = config.Slots[dataIdx];
                bool isLeft = (layoutIdx % 2) == 0;
                if (slotData.Wide) {
                    if (!isLeft) {
                        layout.Slots[layoutIdx].gameObject.SetActive(false);
                        layoutIdx++;
                    }
                    layoutSlot = layout.Slots[layoutIdx];
                    layout.Slots[layoutIdx + 1].gameObject.SetActive(false);
                    layoutIdx += 2;
                    layoutWidth = layout.SlotWidth * 2 + layout.SlotSpacing;
                } else {
                    layoutSlot = layout.Slots[layoutIdx];
                    layoutIdx++;
                    layoutWidth = layout.SlotWidth;
                }

                StringHash32 id = data.AllocatedScraps[dataIdx];
                if (id.IsEmpty) {
                    layoutSlot.gameObject.SetActive(false);
                } else {
                    layoutSlot.gameObject.SetActive(true);
                    layoutSlot.RectTransform().SetSizeDelta(layoutWidth, Axis.X);
                    var scrapData = Assets.Scrap(id);
                    GameText.PopulateStoryScrap(layoutSlot, scrapData, Assets.Style("snippet-in-story"));
                    layoutSlot.ImageOnly.SetActive(StoryScrapData.ShouldContainImage(scrapData.Type));
                }
            }

            for(; layoutIdx < layout.Slots.Length; layoutIdx++) {
                layout.Slots[layoutIdx].gameObject.SetActive(false);
            }

            layout.Headline.SetText(config.FinalHeadline);
        }

        static public void CullFeedback(RingBuffer<ImpactLayout.Item> items, int max) {
            RNG.Instance.Shuffle((IRingBuffer<ImpactLayout.Item>) items);
            using(PooledSet<StringHash32> allocatedLocations = PooledSet<StringHash32>.Create())
            using(PooledSet<StringHash32> allocatedSnippets = PooledSet<StringHash32>.Create()) {
                
                // first pass - try to get one per location and snippet
                for(int i = items.Count - 1; i >= 0; i--) {
                    ref var item = ref items[i];
                    if (item.Locked || item.SnippetId.IsEmpty) {
                        continue;
                    }

                    if (allocatedLocations.Contains(item.Location)) {
                        items.FastRemoveAt(i);
                    } else if (allocatedSnippets.Add(item.SnippetId)) {
                        allocatedLocations.Add(item.Location);
                        item.Locked = true;
                    }
                }

                // second pass - allow multiple per snippet
                for(int i = items.Count - 1; i >= 0; i--) {
                    ref var item = ref items[i];
                    if (item.Locked) {
                        continue;
                    }

                    if (allocatedLocations.Contains(item.Location)) {
                        items.FastRemoveAt(i);
                    } else {
                        allocatedSnippets.Add(item.SnippetId);
                        allocatedLocations.Add(item.Location);
                        item.Locked = true;
                    }
                }

                while(items.Count > max) {
                    items.PopBack();
                }
            }
        }

        static public void LayoutFeedback(ImpactLayout layout) {
            // TODO: Assign to correct locations

            for(int i = 0; i < layout.Pins.Length; i++) {
                var pin = layout.Pins[i];
                if (i >= layout.Items.Count) {
                    pin.Root.gameObject.SetActive(false);
                    return;
                }

                var item = layout.Items[i];
                pin.Line.gameObject.SetActive(true);
                GameText.PopulateTextLine(pin.Line, item.RichText, null, default, Assets.Style(item.Style));
                GameText.AlignTextLine(pin.Line, pin.Alignment);

                switch(pin.Alignment) {
                    case TextAlignment.Left: {
                        pin.LineAnchor.SetAnchorPos(-layout.PinLineOffset, Axis.X);
                        CanvasUtility.SetAnchorX(pin.Line.Root, 0);
                        GameText.SetTailMode(pin.Line, TextLine.TailMode.Left);
                        break;
                    }
                    case TextAlignment.Center: {
                        pin.LineAnchor.SetAnchorPos(0, Axis.X);
                        CanvasUtility.SetAnchorX(pin.Line.Root, 0.5f);
                        GameText.SetTailMode(pin.Line, TextLine.TailMode.Hidden);
                        break;
                    }
                    case TextAlignment.Right: {
                        pin.LineAnchor.SetAnchorPos(layout.PinLineOffset, Axis.X);
                        CanvasUtility.SetAnchorX(pin.Line.Root, 1);
                        GameText.SetTailMode(pin.Line, TextLine.TailMode.Right);
                        break;
                    }
                }

                GameText.PrepareTextLine(pin.Line, 0.5f);
            }
        }

        static public IEnumerator AnimateFeedback(ImpactLayout layout) {
            // TODO: Get the correct pins for the items
            RNG.Instance.Shuffle(layout.Pins, 0, layout.Items.Count);

            for(int i = 0; i < layout.Items.Count; i++) {
                var pin = layout.Pins[i];
                yield return GameText.AnimateTextLinePinnedShow(pin.Line, new Vector2(0, -16), 0.2f);
                yield return 0.3f;
            }
        }
    }
}