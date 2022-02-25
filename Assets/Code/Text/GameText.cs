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
using BeauUtil.Variants;

namespace Journalism {
    static public class GameText {

        static private readonly char[] QuoteTrim = new char[] { '"' };

        #region Lines

        /// <summary>
        /// Populates the contents of a given text line.
        /// </summary>
        static public void PopulateTextLine(TextLine line, StringSlice textString, Sprite icon, Color iconColor, TextStyles.StyleData style) {
            Assert.True(line.gameObject.activeInHierarchy, "TextLine must be active before calling PopulateTextLine");

            textString = textString.Trim(QuoteTrim);

            if (!textString.IsEmpty) {
                line.Text.SetText(textString.ToString());
                line.Text.gameObject.SetActive(true);
            } else {
                line.Text.gameObject.SetActive(false);
                line.Text.SetText(string.Empty);
            }

            if (line.Icon != null) {
                if (icon) {
                    line.Icon.sprite = icon;
                    line.Icon.color = iconColor;
                    line.Icon.gameObject.SetActive(true);
                } else {
                    line.Icon.gameObject.SetActive(false);
                    line.Icon.sprite = null;
                }
            }

            if (style != null) {
                SetTextLineStyle(line, style);
            }

            line.Layout.ForceRebuild();
            line.Root.SetSizeDelta(line.Local.sizeDelta);
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

            for(int i = 0; i < line.Rounding.Length; i++) {
                line.Rounding[i].pixelsPerUnitMultiplier = style.RoundingScale;
            }

            if (line.Pattern != null) {
                for(int i = 0; i < line.Pattern.Length; i++) {
                    line.Pattern[i].texture = style.Texture;
                    line.Pattern[i].gameObject.SetActive(style.Texture);
                }
            }

            if (line.PatternMask != null) {
                line.PatternMask.enabled = style.Texture;
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
            RectTransform innerTail = line.InnerTail;

            if (!tail || !innerTail) {
                return;
            }

            if (tailMode == TextLine.TailMode.Hidden) {
                tail.gameObject.SetActive(false);
                innerTail.gameObject.SetActive(false);
                return;
            }

            tail.gameObject.SetActive(true);
            innerTail.gameObject.SetActive(true);
            Vector2 offset = tail.anchoredPosition,
                innerOffset = innerTail.anchoredPosition;

            switch(tailMode) {
                case TextLine.TailMode.Left: {
                    CanvasUtility.SetAnchor(tail, TextAnchor.LowerLeft);
                    CanvasUtility.SetAnchor(innerTail, TextAnchor.LowerLeft);
                    offset.x = Math.Abs(offset.x);
                    innerOffset.x = Math.Abs(innerOffset.x);
                    break;
                }

                case TextLine.TailMode.Right: {
                    CanvasUtility.SetAnchor(tail, TextAnchor.LowerRight);
                    CanvasUtility.SetAnchor(innerTail, TextAnchor.LowerRight);
                    offset.x = -Math.Abs(offset.x);
                    innerOffset.x = -Math.Abs(innerOffset.x);
                    break;
                }
            }

            tail.anchoredPosition = offset;
            innerTail.anchoredPosition = innerOffset;
        }

        #endregion // Lines

        #region Scroll

        /// <summary>
        /// Initializes the scroll line.
        /// </summary>
        static public void InitializeScroll(TextLineScroll scroll) {
            scroll.Lines = new RingBuffer<TextLineScroll.LineState>(scroll.MaxLines + 8, RingBufferMode.Fixed);
            scroll.RootBaseline = scroll.Root.anchoredPosition.y;
        }

        /// <summary>
        /// Allocates a new line.
        /// </summary>
        static public TempAlloc<TextLine> AllocLine(TextPools pools) {
            TempAlloc<TextLine> lineAlloc = pools.LinePool.TempAlloc();
            return lineAlloc;
        }

        /// <summary>
        /// Allocates a new line.
        /// </summary>
        static public TextLine AllocLine(TextLineScroll scroll, TextPools pools) {
            TempAlloc<TextLine> lineAlloc = pools.LinePool.TempAlloc();
            TextLine line = lineAlloc.Object;
            RectTransform lineRect = line.Root;
            lineRect.SetParent(scroll.ListRoot, false);
            lineRect.anchoredPosition = default;
            line.Local.anchoredPosition = default;
            lineRect.SetRotation(RNG.Instance.NextFloat(-scroll.RotationRange, scroll.RotationRange), Axis.Z, Space.Self);
            line.Group.alpha = 0;
            scroll.Lines.PushFront(new TextLineScroll.LineState() {
                LineAlloc = lineAlloc,
                Line = line
            });
            return line;
        }

        /// <summary>
        /// Allocates a new story scrap.
        /// </summary>
        static public TempAlloc<StoryScrapDisplay> AllocScrap(StoryScrapData scrap, TextPools pools) {
            TempAlloc<StoryScrapDisplay> scrapAlloc;
            if (scrap.Type == StoryScrapType.Picture || scrap.Type == StoryScrapType.Graph) {
                scrapAlloc = pools.ImageStoryPool.TempAlloc();
            } else {
                scrapAlloc = pools.TextStoryPool.TempAlloc();
            }
            return scrapAlloc;
        }

        /// <summary>
        /// Allocates a new story scrap.
        /// </summary>
        static public StoryScrapDisplay AllocScrap(StoryScrapData scrap, TextLineScroll scroll, TextPools pools) {
            TempAlloc<StoryScrapDisplay> scrapAlloc;
            if (scrap.Type == StoryScrapType.Picture || scrap.Type == StoryScrapType.Graph) {
                scrapAlloc = pools.ImageStoryPool.TempAlloc();
            } else {
                scrapAlloc = pools.TextStoryPool.TempAlloc();
            }

            TextLine line = scrapAlloc.Object.Line;
            RectTransform lineRect = line.Root;
            lineRect.SetParent(scroll.ListRoot, false);
            lineRect.anchoredPosition = default;
            line.Local.anchoredPosition = default;
            lineRect.SetRotation(RNG.Instance.NextFloat(-scroll.RotationRange, scroll.RotationRange), Axis.Z, Space.Self);
            line.Group.alpha = 0;
            scroll.Lines.PushFront(new TextLineScroll.LineState() {
                ScrapAlloc = scrapAlloc,
                Line = line
            });
            return scrapAlloc.Object;
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
                line = state.Line;
                size = line.Root.sizeDelta;
                width = size.x;
                height = size.y;
                if (line.Tail && line.Tail.gameObject.activeSelf) {
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
        /// Adjusts computed line positions based on a fixed amount.
        /// </summary>
        static public void AdjustComputedLocationsFixed(TextLineScroll scroll, float adjust) {
            var states = scroll.Lines;
            int count = states.Count;

            for(int i = 0; i < count; i++) {
                ref var state = ref states[i];
                state.LocationY += adjust;
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
                line = state.Line;
                size = line.Root.sizeDelta;
                width = size.x;
                height = size.y;
                if (line.Tail && line.Tail.gameObject.activeSelf) {
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
        /// Animates all lines to their appropriate locations.
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
                line = state.Line;

                delay = (newLineCount - i) * scroll.TextScrollDelay;

                line.Root.SetAnchorPos(state.LocationX, Axis.X);

                anim = line.Root.AnchorPosTo(state.LocationY, scroll.NewTextAnimParams, Axis.Y).DelayBy(delay).Play(line);
                state.LocationAnimation = anim;
                
                anim = line.Group.FadeTo(1, scroll.NewTextAnimParams.Time).DelayBy(delay).Play(line);
                state.RevealAnimation = anim;
            }

            for(int i = newLineCount; i < count; i++) {
                ref var state = ref states[i];
                line = state.Line;

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
                line = state.Line;

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
                state.LineAlloc?.Dispose();
                state.ScrapAlloc?.Dispose();
                state.LocationAnimation.Stop();
                state.RevealAnimation.Stop();
                state.StyleAnimation.Stop();
            }
        }

        /// <summary>
        /// Clears all lines from the given scroll.
        /// </summary>
        static public void ClearLines(TextLineScroll scroll) {
            var states = scroll.Lines;

            while(states.Count > 0) {
                var state = states.PopBack();
                state.LineAlloc?.Dispose();
                state.ScrapAlloc?.Dispose();
                state.LocationAnimation.Stop();
                state.RevealAnimation.Stop();
                state.StyleAnimation.Stop();
            }
        }

        #endregion // Scroll
    
        #region Choice

        static public void InitializeChoices(TextChoiceGroup choices) {
            choices.DefaultChoiceGroup.alpha = 0;
            choices.DefaultChoiceGroup.blocksRaycasts = false;
            choices.GridGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Allocates a new line.
        /// </summary>
        static public TextChoice AllocChoice(TextChoiceGroup choices, TextPools pools) {
            TempAlloc<TextChoice> choiceAlloc = pools.ChoicePool.TempAlloc();
            TextChoice choice = choiceAlloc.Object;
            TextLine line = choice.Line;
            RectTransform lineRect = line.Root;
            lineRect.SetParent(choices.GridRoot, false);
            lineRect.anchoredPosition = default;
            line.Local.anchoredPosition = default;
            lineRect.SetRotation(RNG.Instance.NextFloat(-choices.RotationRange, choices.RotationRange), Axis.Z, Space.Self);
            line.Group.alpha = 0;
            choices.Choices.PushBack(new TextChoiceGroup.ChoiceState() {
                Choice = choiceAlloc
            });
            return choice;
        }

        /// <summary>
        /// Populates the contents of a given choice.
        /// </summary>
        static public void PopulateChoice(TextChoice choice, StringSlice textString, Variant targetId, uint timeCost, TextStyles.StyleData style) {
            Assert.True(choice.gameObject.activeInHierarchy, "TextChoice must be active before calling PopulateTextLine");

            textString = textString.Trim(QuoteTrim);

            TextLine line = choice.Line;
            line.Text.SetText(textString.ToString());
            line.Text.gameObject.SetActive(true);

            choice.TargetId = targetId;
            choice.TimeCost = timeCost;
            choice.Selected = false;

            if (line.Icon != null) {
                if (timeCost > 0) {
                    line.Icon.gameObject.SetActive(true);
                    line.Icon.fillAmount = (float) timeCost / Stats.TimeUnitsPerHour;
                } else {
                    line.Icon.gameObject.SetActive(false);
                }
            }

            if (style != null) {
                SetTextLineStyle(choice.Line, style);
            }
        }

        /// <summary>
        /// Recomputes all line positions in the given scroll.
        /// </summary>
        static public void RecomputeAllLocations(TextChoiceGroup choices) {
            var states = choices.Choices;
            int count = states.Count;
            float spacingX = choices.ColumnSpacing;
            float spacingY = choices.RowSpacing;

            int rowCount, columnCount;
            if (count <= 2) {
                columnCount = count;
                rowCount = 1;
            } else {
                columnCount = (int) Math.Ceiling(count / 2f);
                rowCount = 2;
            }

            float xOffset = -(columnCount - 1) * choices.ColumnSpacing * 0.5f;
            float yOffset = (rowCount - 1) * choices.RowSpacing * 0.5f;

            for(int i = 0; i < count; i++) {
                ref var state = ref states[i];
                int x = i % columnCount;
                int y = i / columnCount;

                state.LocationX = xOffset + spacingX * x;;
                state.LocationY = yOffset - spacingY * y;
            }
        }

        /// <summary>
        /// Animates all choices to their appropriate locations.
        /// </summary>
        static public IEnumerator AnimateLocations(TextChoiceGroup choices) {
            var states = choices.Choices;
            int count = states.Count;

            TextChoice choice;
            Routine anim;
            float delay = 0;

            for(int i = 0; i < count; i++) {
                ref var state = ref states[i];
                choice = state.Choice.Object;

                delay = i * choices.NewScrollDelay;

                choice.Line.Root.SetAnchorPos(new Vector2(state.LocationX, state.LocationY - choices.NewAnimDistance), Axis.XY);

                anim = choice.Line.Root.AnchorPosTo(state.LocationY, choices.NewChoiceAnimParams, Axis.Y).DelayBy(delay).Play(choice);
                state.LocationAnimation = anim;
                
                anim = choice.Line.Group.FadeTo(1, choices.NewChoiceAnimParams.Time).DelayBy(delay).Play(choice);
                state.RevealAnimation = anim;
            }

            return Routine.WaitSeconds(delay + choices.NewChoiceAnimParams.Time);
        }

        static public IEnumerator WaitForChoice(TextChoiceGroup choices, LeafChoice choice) {
            choices.GridGroup.blocksRaycasts = true;
            while(!choice.HasChosen()) {
                foreach(var btnState in choices.Choices) {
                    TextChoice choiceButton = btnState.Choice.Object;
                    if (choiceButton.Selected) {
                        choice.Choose(choiceButton.TargetId);
                        break;
                    }
                }
                yield return null;
            }
            choices.GridGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// Animates all lines to a vanished state.
        /// </summary>
        static public IEnumerator AnimateVanish(TextChoiceGroup choices) {
            var states = choices.Choices;
            int count = states.Count;

            TextLine line;
            Routine anim;
            float delay = 0;

            for(int i = 0; i < count; i++) {
                ref var state = ref states[i];
                line = state.Choice.Object.Line;

                delay = (count - 1 - i) * choices.TextVanishDelay;

                anim = line.Root.AnchorPosTo(state.LocationY + choices.VanishAnimDistance, choices.VanishAnimParams, Axis.Y).DelayBy(delay).Play(line);
                state.LocationAnimation = anim;

                anim = line.Group.FadeTo(0, choices.VanishAnimParams.Time).DelayBy(delay).Play(line);
                state.RevealAnimation = anim;
            }

            return Routine.WaitSeconds(count * choices.TextVanishDelay + choices.VanishAnimParams.Time);
        }

        /// <summary>
        /// Clears choices overflowing from the choice group.
        /// </summary>
        static public void ClearChoices(TextChoiceGroup choices) {
            var states = choices.Choices;

            while(states.Count > 0) {
                var state = states.PopBack();
                state.Choice.Dispose();
                state.LocationAnimation.Stop();
                state.RevealAnimation.Stop();
            }
        }

        #region Defaults

        static public IEnumerator WaitForDefaultNext(TextChoiceGroup choices, TextStyles.StyleData style) {
            PopulateTextLine(choices.DefaultNextButton.Line, null, choices.DefaultNextIcon, choices.DefaultNextIconColor, style);
            choices.DefaultNextButton.transform.SetRotation(RNG.Instance.NextFloat(-choices.RotationRange, choices.RotationRange), Axis.Z, Space.Self);
            yield return choices.DefaultChoiceGroup.FadeTo(1, choices.NewChoiceAnimParams.Time / 2);
            choices.DefaultChoiceGroup.blocksRaycasts = true;
            choices.DefaultNextButton.Selected = false;
            while(!choices.DefaultNextButton.Selected) {
                yield return null;
            }
            choices.DefaultChoiceGroup.blocksRaycasts = false;
            yield return choices.DefaultChoiceGroup.FadeTo(0, choices.VanishAnimParams.Time / 2);
        }

        static public IEnumerator WaitForPlayerNext(TextChoiceGroup choices, string text, TextStyles.StyleData style) {
            PopulateTextLine(choices.DefaultNextButton.Line, text, null, default, style);

            choices.DefaultNextButton.transform.SetRotation(RNG.Instance.NextFloat(-choices.RotationRange, choices.RotationRange), Axis.Z, Space.Self);
            yield return choices.DefaultChoiceGroup.FadeTo(1, choices.NewChoiceAnimParams.Time / 2);
            choices.DefaultChoiceGroup.blocksRaycasts = true;
            choices.DefaultNextButton.Selected = false;
            while(!choices.DefaultNextButton.Selected) {
                yield return null;
            }
            choices.DefaultChoiceGroup.blocksRaycasts = false;
            yield return choices.DefaultChoiceGroup.FadeTo(0, choices.VanishAnimParams.Time / 2);
        }

        #endregion // Defaults

        #endregion // Choice

        #region Story Scraps

        /// <summary>
        /// Populates a story scrap.
        /// </summary>
        static public void PopulateStoryScrap(StoryScrapDisplay display, StoryScrapData data, TextStyles.StyleData style) {
            if (display.Texture) {
                display.Texture.Path = data.ImagePath;
            }

            // TODO: Localization
            if (display.Line.Text) {
                display.Line.Text.SetText(data.Content);
            }

            SetTextLineStyle(display.Line, style);

            if (display.Line.Layout) {
                display.Line.Layout.ForceRebuild();
                display.Line.Root.SetSizeDelta(display.Line.Local.sizeDelta);
            }
        }

        #endregion // Story Scraps

        #region Parsing

        static public class Events {
            static public readonly StringHash32 Background = "background";
            static public readonly StringHash32 Image = "image";
            static public readonly StringHash32 Anim = "animation";
            static public readonly StringHash32 Auto = "auto";
            static public readonly StringHash32 ClearImage = "clear-image";
            static public readonly StringHash32 BackgroundFadeOut = "background-fadeout";
            static public readonly StringHash32 BackgroundFadeIn = "background-fadein";
        }

        static public class TextAnims {
            static public readonly StringHash32 Sway = "sway";
            static public readonly StringHash32 Rumble = "rumble";
        }

        static public class ChoiceData {
            static public readonly StringHash32 Time = "time";
            static public readonly StringHash32 Once = "once";
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

            config.AddReplace("timeLeft", () => FormatTime(Player.Data.TimeRemaining));

            config.AddEvent("bg", Events.Background).WithStringData();
            config.AddEvent("anim", Events.Anim).WithStringData();
            config.AddEvent("auto", Events.Auto).WithFloatData(0.2f);
            config.AddEvent("bg-fadeout", Events.BackgroundFadeOut).WithStringData();
            config.AddEvent("bg-fadein", Events.BackgroundFadeIn).WithStringData();
            config.AddEvent("img", Events.Image).WithStringData().CloseWith(Events.ClearImage);

            textDisplay.ConfigureHandlers(config, handler);
            visuals.ConfigureHandlers(config, handler);
        }

        /// <summary>
        /// Finds the character associated with the given TagString line.
        /// </summary>
        static public StringHash32 FindCharacter(TagString line) {
            line.TryFindEvent(LeafUtils.Events.Character, out TagEventData characterEvt);
            return characterEvt.GetStringHash();
        }

        /// <summary>
        /// Finds the animations associated with the given TagString line.
        /// </summary>
        static public StringSlice FindAnims(TagString line) {
            line.TryFindEvent(Events.Anim, out TagEventData characterEvt);
            return characterEvt.StringArgument;
        }

        #endregion // Parsing

        #region Text Generation

        static public string FormatTime(uint timeRemaining) {
            uint hours = timeRemaining / Stats.TimeUnitsPerHour;
            uint minutes = Stats.MinutesPerTimeUnit * (timeRemaining % Stats.TimeUnitsPerHour);

            string hourStr = null, minuteStr = null;
            
            // TODO: Localization
            if (hours > 1) {
                hourStr = string.Format("<color=#EB8686>{0}</color> hours", hours);
            } else if (hours == 1) {
                hourStr = "<color=#EB8686>1</color> hour";
            }

            if (minutes > 0) {
                minuteStr = string.Format("<color=#EB8686>{0}</color> minutes", minutes);
            } else if (hours == 0) {
                minuteStr = "<color=#EB8686>0</color> minutes";
            }

            if (hourStr != null && minuteStr != null) {
                return string.Join(" and ", hourStr, minuteStr);
            } else {
                return hourStr ?? minuteStr;
            }
        }

        #endregion // Text Generation
    
        #region Text Styles

        static public void SetAnim(TextLine line, StringSlice anims, ref Routine anim) {
            // TODO: Parse styles to allow multiple anims
            StringHash32 styleId = anims.Hash32();
            if (styleId == TextAnims.Rumble) {
                anim.Replace(line, TextAnim_Rumble(line));
            } else if (styleId == TextAnims.Sway) {
                anim.Replace(line, TextAnim_Sway(line));
            }
        }

        static private IEnumerator TextAnim_Rumble(TextLine line) {
            return Routine.Combine(
                line.Local.AnchorPosTo(1, 0.097f, Axis.X).Randomize().RevertOnCancel().Loop().Wave(Wave.Function.Sin, 1),
                line.Local.AnchorPosTo(1, 0.1117f, Axis.Y).Randomize().RevertOnCancel().Loop().Wave(Wave.Function.Sin, 1)
            );
        }

        static private IEnumerator TextAnim_Sway(TextLine line) {
            return Routine.Combine(
                line.Local.AnchorPosTo(4, 1, Axis.X).Randomize().RevertOnCancel().Loop().Wave(Wave.Function.Sin, 1),
                line.Local.AnchorPosTo(1, 2, Axis.Y).Randomize().RevertOnCancel().Loop().Wave(Wave.Function.Sin, 1)
            );
        }

        #endregion // Text Styles

        // TODO: Stat changes
    }
}