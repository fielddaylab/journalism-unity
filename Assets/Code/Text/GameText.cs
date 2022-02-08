using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauUtil.Debugger;
using BeauRoutine;
using System;
using System.Collections;
using BeauPools;
using BeauUtil.Tags;
using Leaf;

namespace Journalism {
    static public class GameText {

        #region Lines

        /// <summary>
        /// Populates the contents of a given text line.
        /// </summary>
        static public void PopulateTextLine(TextLine line, StringSlice textString, Sprite icon, Color iconColor, TextStyles.StyleData style) {
            Assert.True(line.gameObject.activeInHierarchy, "TextLine must be active before calling PopulateTextLine");

            if (!textString.IsEmpty) {
                line.Text.SetText(textString.ToString());
                line.Text.gameObject.SetActive(true);
            } else {
                line.Text.gameObject.SetActive(false);
                line.Text.SetText(string.Empty);
            }

            if (icon) {
                line.Icon.sprite = icon;
                line.Icon.color = iconColor;
                line.Icon.gameObject.SetActive(true);
            } else {
                line.Icon.gameObject.SetActive(false);
                line.Icon.sprite = null;
            }

            if (style != null) {
                SetTextLineStyle(line, style);
            }

            line.Layout.ForceRebuild();
        }

        /// <summary>
        /// Sets the line style of the given text.
        /// </summary>
        static public void SetTextLineStyle(TextLine line, TextStyles.StyleData style) {
            if (line.CurrentStyle == style) {
                return;
            }

            line.CurrentStyle = style;
            
            for(int i = 0; i < line.BackgroundColor.Length; i++) {
                line.BackgroundColor[i].SetColor(style.Background);
            }

            for(int i = 0; i < line.OutlineColor.Length; i++) {
                line.OutlineColor[i].SetColor(style.Outline);
            }

            SetTailMode(line, style.Tail);
        }

        static public TextAlignment ComputeDesiredAlignment(TextLine line, TextLineScroll scroll) {
            TextLine.TailMode tail = line.CurrentStyle?.Tail ?? TextLine.TailMode.Hidden;
            switch(tail) {
                case TextLine.TailMode.Left: {
                    return TextAlignment.Left;
                }
                case TextLine.TailMode.Right: {
                    return TextAlignment.Right;
                }
                default: {
                    return scroll.Alignment;
                }
            }
        }

        static public void AlignTextLine(TextLine line, TextAlignment alignment) {
            switch(alignment) {
                case TextAlignment.Left: {
                    CanvasUtility.SetAnchor(line.Root, TextAnchor.LowerLeft);
                    break;
                }

                case TextAlignment.Center: {
                    CanvasUtility.SetAnchor(line.Root, TextAnchor.LowerCenter);
                    break;
                }

                case TextAlignment.Right: {
                    CanvasUtility.SetAnchor(line.Root, TextAnchor.LowerRight);
                    break;
                }
            }
            
            line.CurrentAlignment = alignment;
        }

        static private void SetTailMode(TextLine line, TextLine.TailMode tailMode) {
            RectTransform tail = line.Tail;

            if (tailMode == TextLine.TailMode.Hidden) {
                tail.gameObject.SetActive(false);
                return;
            }

            tail.gameObject.SetActive(true);
            Vector2 anchorMin = tail.anchorMin,
                anchorMax = tail.anchorMax,
                offset = tail.anchoredPosition;

            switch(tailMode) {
                case TextLine.TailMode.Left: {
                    anchorMin.x = anchorMax.x = 0;
                    offset.x = Math.Abs(offset.x);
                    break;
                }

                case TextLine.TailMode.Right: {
                    anchorMin.x = anchorMax.x = 1;
                    offset.x = -Math.Abs(offset.x);
                    break;
                }
            }

            tail.anchorMin = anchorMin;
            tail.anchorMax = anchorMax;
            tail.anchoredPosition = offset;
        }

        #endregion // Lines

        #region Scroll

        /// <summary>
        /// Initializes the scroll line.
        /// </summary>
        static public void InitializeScroll(TextLineScroll scroll) {
            scroll.Lines = new RingBuffer<TextLineScroll.LineState>(scroll.MaxLines + 3, RingBufferMode.Fixed);
        }

        /// <summary>
        /// Allocates a new line.
        /// </summary>
        static public TextLine AllocLine(TextLineScroll scroll, TextPools pools) {
            TempAlloc<TextLine> lineAlloc = pools.LinePool.TempAlloc<TextLine>();
            TextLine line = lineAlloc.Object;
            RectTransform lineRect = line.Root;
            lineRect.SetParent(scroll.ListRoot, false);
            lineRect.anchoredPosition = default;
            lineRect.SetRotation(RNG.Instance.NextFloat(-scroll.RotationRange, scroll.RotationRange), Axis.Z, Space.Self);
            line.Group.alpha = 0;
            scroll.Lines.PushFront(new TextLineScroll.LineState() {
                Line = lineAlloc
            });
            return line;
        }

        /// <summary>
        /// Adjusts computed line positions based on computed positions of new lines.
        /// </summary>
        static public void AdjustComputedLocations(TextLineScroll scroll, int newLineCount) {
            var states = scroll.Lines;
            int count = states.Count;
            float spacing = scroll.Spacing;

            Assert.True(newLineCount <= count);

            TextLine line;
            float baseline = 0;
            Vector2 size;
            float width;
            float height;
            float extraOffset;
            float inset;
            float x;
            for(int i = 0; i < newLineCount; i++) {
                ref var state = ref states[i];
                line = state.Line.Object;
                size = line.Root.sizeDelta;
                width = size.x;
                height = size.y;
                if (line.Tail.gameObject.activeSelf) {
                    extraOffset = line.TailHeight;
                    inset = scroll.DialogInset;
                } else {
                    extraOffset = 0;
                    inset = 0;
                }

                x = 0;
                switch(line.CurrentAlignment) {
                    case TextAlignment.Left: {
                        x = width / 2 + inset;
                        break;
                    }
                    case TextAlignment.Right: {
                        x = -width / 2 - inset;
                        break;
                    }
                }

                state.LocationX = x;
                state.LocationY = baseline + extraOffset + height / 2;
                baseline = baseline + height + extraOffset + spacing;
            }

            for(int i = newLineCount; i < count; i++) {
                ref var state = ref states[i];
                state.LocationY += baseline;
            }
        }

        /// <summary>
        /// Recomputes all line positions in the given scroll.
        /// </summary>
        static public void RecomputeAllLocations(TextLineScroll scroll) {
            var states = scroll.Lines;
            int count = states.Count;
            float spacing = scroll.Spacing;

            TextLine line;
            float baseline = 0;
            Vector2 size;
            float width;
            float height;
            float extraOffset;
            float inset;
            float x;
            for(int i = 0; i < count; i++) {
                ref var state = ref states[i];
                line = state.Line.Object;
                size = line.Root.sizeDelta;
                width = size.x;
                height = size.y;
                if (line.Tail.gameObject.activeSelf) {
                    extraOffset = line.TailHeight;
                    inset = scroll.DialogInset;
                } else {
                    extraOffset = 0;
                    inset = 0;
                }

                x = 0;
                switch(line.CurrentAlignment) {
                    case TextAlignment.Left: {
                        x = width / 2 + inset;
                        break;
                    }
                    case TextAlignment.Right: {
                        x = -width / 2 - inset;
                        break;
                    }
                }

                state.LocationX = x;
                state.LocationY = baseline + extraOffset + height / 2;
                baseline = baseline + height + extraOffset + spacing;
            }
        }

        /// <summary>
        /// Animates all 
        /// </summary>
        static public IEnumerator AnimateLocations(TextLineScroll scroll, int newLineCount) {
            var states = scroll.Lines;
            int count = states.Count;

            Assert.True(newLineCount <= count);

            TextLine line;
            Routine anim;
            float delay = 0;
            for(int i = 0; i < newLineCount; i++) {
                ref var state = ref states[i];
                line = state.Line.Object;

                delay = (newLineCount - i) * scroll.TextScrollDelay;

                line.Root.SetAnchorPos(state.LocationX, Axis.X);

                anim = line.Root.AnchorPosTo(state.LocationY, scroll.NewTextAnimParams, Axis.Y).DelayBy(delay).Play(line);
                state.LocationAnimation = anim;
                
                anim = line.Group.FadeTo(1, scroll.NewTextAnimParams.Time).DelayBy(delay).Play(line);
                state.RevealAnimation = anim;
            }

            for(int i = newLineCount; i < count; i++) {
                ref var state = ref states[i];
                line = state.Line.Object;

                delay = i * scroll.TextScrollDelay;

                anim = line.Root.AnchorPosTo(state.LocationY, scroll.TextScrollAnimParams, Axis.Y).DelayBy(delay).Play(line);
                state.LocationAnimation = anim;
            }

            return Routine.WaitSeconds(delay + scroll.TextScrollAnimParams.Time);
        }

        /// <summary>
        /// Animates all lines to a vanished state.
        /// </summary>
        static public IEnumerator AnimateVanish(TextLineScroll scroll) {
            var states = scroll.Lines;
            int count = states.Count;

            TextLine line;
            Routine anim;
            float delay = 0;

            for(int i = 0; i < count; i++) {
                ref var state = ref states[i];
                line = state.Line.Object;

                delay = (count - 1 - i) * scroll.TextVanishDelay;

                anim = line.Root.AnchorPosTo(state.LocationY + scroll.VanishAnimDistance, scroll.VanishAnimParams, Axis.Y).DelayBy(delay).Play(line);
                state.LocationAnimation = anim;

                anim = line.Group.FadeTo(0, scroll.VanishAnimParams.Time).DelayBy(delay).Play(line);
                state.RevealAnimation = anim;
            }

            return Routine.WaitSeconds(count * scroll.TextVanishDelay + scroll.VanishAnimParams.Time);
        }

        /// <summary>
        /// Clears lines overflowing from the given scroll.
        /// </summary>
        static public void ClearOverflowLines(TextLineScroll scroll) {
            var states = scroll.Lines;

            while(states.Count > scroll.MaxLines) {
                var state = states.PopBack();
                state.Line.Dispose();
                state.LocationAnimation.Stop();
                state.RevealAnimation.Stop();
            }
        }

        /// <summary>
        /// Clears lines overflowing from the given scroll.
        /// </summary>
        static public void ClearLines(TextLineScroll scroll) {
            var states = scroll.Lines;

            while(states.Count > 0) {
                var state = states.PopBack();
                state.Line.Dispose();
                state.LocationAnimation.Stop();
                state.RevealAnimation.Stop();
            }
        }

        #endregion // Scroll
    
        #region Choice

        static public void InitializeChoices(TextChoiceGroup choices) {
            choices.DefaultChoiceGroup.alpha = 0;
            choices.DefaultChoiceGroup.blocksRaycasts = false;
        }

        static public IEnumerator WaitForDefaultNext(TextChoiceGroup choices, TextStyles.StyleData style) {
            PopulateTextLine(choices.DefaultNextButton.Line, null, choices.DefaultNextIcon, choices.DefaultNextIconColor, style);
            choices.DefaultNextButton.transform.SetRotation(RNG.Instance.NextFloat(-1, 1), Axis.Z, Space.Self);
            yield return choices.DefaultChoiceGroup.FadeTo(1, 0.1f);
            choices.DefaultChoiceGroup.blocksRaycasts = true;
            yield return choices.DefaultNextButton.Button.onClick.WaitForInvoke();
            choices.DefaultChoiceGroup.blocksRaycasts = false;
            yield return choices.DefaultChoiceGroup.FadeTo(0, 0.1f);
        }

        static public IEnumerator WaitForPlayerNext(TextChoiceGroup choices, string text, TextStyles.StyleData style) {
            PopulateTextLine(choices.DefaultNextButton.Line, text, null, default, style);
            choices.DefaultNextButton.transform.SetRotation(RNG.Instance.NextFloat(-1, 1), Axis.Z, Space.Self);
            yield return choices.DefaultChoiceGroup.FadeTo(1, 0.1f);
            choices.DefaultChoiceGroup.blocksRaycasts = true;
            yield return choices.DefaultNextButton.Button.onClick.WaitForInvoke();
            choices.DefaultChoiceGroup.blocksRaycasts = false;
            yield return choices.DefaultChoiceGroup.FadeTo(0, 0.1f);
        }

        #endregion // Choice

        #region Parsing

        static public class Events {
            static public readonly StringHash32 Character = "character";
            static public readonly StringHash32 Background = "background";
            static public readonly StringHash32 Image = "image";
            static public readonly StringHash32 ClearImage = "clear-image";
            static public readonly StringHash32 BackgroundFadeOut = "background-fadeout";
            static public readonly StringHash32 BackgroundFadeIn = "background-fadein";
        }

        static public class Characters {
            static public readonly StringHash32 Me = "me";
            static public readonly StringHash32 Action = "action";
        }

        static public bool IsPlayer(StringHash32 characterId) {
            return characterId == Characters.Me || characterId == Characters.Action;
        }

        /// <summary>
        /// Initializes event parsing.
        /// </summary>
        static public void InitializeEvents(CustomTagParserConfig config, TagStringEventHandler handler, LeafIntegration integration, ScriptVisualsSystem visuals, TextDisplaySystem textDisplay) {
            LeafUtils.ConfigureDefaultParsers(config, integration, null); // TODO: Replace with appropriate localization callback

            config.AddEvent("@*", Events.Character).ProcessWith(ProcessCharacter);
            config.AddEvent("bg", Events.Background).WithStringData();
            config.AddEvent("bg-fadeout", Events.BackgroundFadeOut).WithStringData();
            config.AddEvent("bg-fadein", Events.BackgroundFadeIn).WithStringData();
            config.AddEvent("img", Events.Image).WithStringData().CloseWith(Events.ClearImage);

            textDisplay.ConfigureHandlers(config, handler);
            visuals.ConfigureHandlers(config, handler);
        }

        static private void ProcessCharacter(TagData tag, object context, ref TagEventData evt) {
            evt.SetStringHash(tag.Id.Substring(1));
        }

        /// <summary>
        /// Finds the character associated with the given TagString line.
        /// </summary>
        static public StringHash32 FindCharacter(TagString line) {
            line.TryFindEvent(GameText.Events.Character, out TagEventData characterEvt);
            return characterEvt.GetStringHash();
        }

        #endregion // Parsing
    }
}