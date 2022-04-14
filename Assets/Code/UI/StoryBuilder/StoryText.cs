using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;

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
    }
}