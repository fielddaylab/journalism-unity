using UnityEngine;
using Leaf.Defaults;
using Leaf;
using Leaf.Runtime;
using System.Collections;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Debugger;
using BeauRoutine;
using BeauPools;
using System;
using Journalism.UI;
using BeauUtil.UI;

namespace Journalism {
    public sealed class TextDisplayLayer : MonoBehaviour, ITextDisplayer, IChoiceDisplayer {

        public enum DesiredColumnState {
            NoChange,
            Unload,
            Reload
        }

        public delegate TagString LookupLineDelegate(StringHash32 id);
        public delegate TagString NextLineDelegate();
        public delegate bool NextChoicesDelegate();

        public delegate DesiredColumnState ColumnNeedsReloadPredicate(TagEventData eventData, StringSlice additionalArg);
        public delegate IEnumerator LoadColumnDelegate(TagEventData eventData, StringSlice additionalArg);
        public delegate void UnloadColumnDelegate();

        #region Inspector

        [SerializeField, Required] public TextPools Pools = null;
        [SerializeField, Required] public TextLineScroll Text = null;
        [SerializeField, Required] public TextChoiceGroup Choices = null;

        [Header("Animation")]
        [SerializeField] private float m_ChoiceRowsOffset = 48;
        [SerializeField] private TweenSettings m_ChoiceRowsAnim = new TweenSettings(0.2f, Curve.Smooth);
        
        [Header("Column")]
        [SerializeField, Required] public AnimatedElement AltColumn = null;
        [SerializeField] private float m_ColumnShift = 250;

        [Header("Options")]
        [SerializeField] private bool m_RequireInputForPlayerText = true;
        [SerializeField] private bool m_AlignDefaultChoiceButtonToText = false;

        #endregion // Inspector

        [NonSerialized] private TextLine m_QueuedLine;
        [NonSerialized] private float m_AutoContinue = -1;
        [NonSerialized] private bool m_ForceNext = false;
        [NonSerialized] private float m_AltColumnBaseline;

        public LookupLineDelegate LookupLine;
        public NextLineDelegate LookupNextLine;
        public NextChoicesDelegate LookupNextChoice;

        public ColumnNeedsReloadPredicate NeedReloadColumn;
        public LoadColumnDelegate LoadColumn;
        public UnloadColumnDelegate UnloadColumn;

        private readonly TagStringEventHandler m_Handlers = new TagStringEventHandler();

        #region Unity

        private void Awake() {
            GameText.InitializeScroll(Text);
            GameText.InitializeChoices(Choices);

            m_AltColumnBaseline = AltColumn.RectTransform.anchoredPosition.y;
            AnimatedElement.Hide(AltColumn);
        }

        #endregion // Unity

        #region Events

        public void ConfigureHandlers() {
            m_Handlers.Register(LeafUtils.Events.Character, HandleCharacter)
                .Register(GameText.Events.Anim, HandleAnim)
                .Register(GameText.Events.Auto, HandleAuto)
                .Register(GameText.Events.Layout, HandleLayout)
                .Register(GameText.Events.ClearText, ClearLines);
        }

        private void HandleCharacter(TagEventData evtData, object context) {
            StringHash32 characterId = evtData.GetStringHash();
            // TODO: Some indirection? Character -> Style as opposed to Character == Style?
            SetStyle(characterId);
            SetChar(characterId);
        }

        private void SetStyle(StringHash32 styleId) {
            var style = Assets.Style(styleId);
            Assert.NotNull(m_QueuedLine, "Cannot set style without any text accompaniment");
            GameText.SetTextLineStyle(m_QueuedLine, style);
        }

        private void SetChar(StringHash32 charId) {
            var charData = Assets.Char(charId);
            Assert.NotNull(m_QueuedLine, "Cannot set character without any text accompaniment");
            GameText.SetTextLineChar(m_QueuedLine, charData);
        }

        private void HandleAnim(TagEventData evtData, object context) {
            if (m_QueuedLine != null) {
                GameText.SetAnim(m_QueuedLine, evtData.StringArgument, ref Text.Lines[0].StyleAnimation);
            }
        }

        private void HandleAuto(TagEventData evtData, object context) {
            m_AutoContinue = evtData.GetFloat();
        }
        
        private IEnumerator HandleLayout(TagEventData evtData, object context) {
            if (evtData.IsClosing) {
                return ResetLayout();
            } else {
                TextAlignment textAlign = StringParser.ConvertTo<TextAlignment>(evtData.StringArgument, TextAlignment.Left);
                return ShiftLayout(textAlign, evtData, null);
            }
        }

        public IEnumerator ShiftLayout(TextAlignment? textAlignOverride, TagEventData evtData = default, StringSlice columnArg = default) {
            TextAlignment textAlignment = textAlignOverride.GetValueOrDefault(Text.Alignment);

            if (textAlignment == TextAlignment.Center) {
                yield return ResetLayout();
                yield break;
            }

            var desiredAltState = NeedReloadColumn?.Invoke(evtData, columnArg) ?? DesiredColumnState.NoChange;
            TextAlignment altAlign = textAlignment.Mirror();

            // if everything is aligned, no need to do anything more.
            bool realignText = textAlignment != Text.Alignment;
            bool columnFadeOut = AnimatedElement.IsActive(AltColumn);
            bool columnContentsChange = desiredAltState != DesiredColumnState.NoChange;

            if (!realignText && !columnFadeOut && !columnContentsChange) {
                yield break;
            }

            AltColumn.Alignment = altAlign;
            Text.Alignment = textAlignment;

            using(PooledList<IEnumerator> anims = PooledList<IEnumerator>.Create()) {
                if (realignText) {
                    anims.Add(ClearLines());
                }
                if (columnFadeOut) {
                    anims.Add(AnimatedElement.Hide(AltColumn, 0.2f));
                }

                yield return Routine.Combine(anims);
                anims.Clear();

                switch(desiredAltState) {
                    case DesiredColumnState.Unload: {
                        UnloadColumn?.Invoke();
                        break;
                    }
                    case DesiredColumnState.Reload: {
                        yield return LoadColumn?.Invoke(evtData, columnArg);
                        break;
                    }
                }

                if (realignText) {
                    float imgX = 0, textX = 0;
                    switch(textAlignment) {
                        case TextAlignment.Left: {
                            textX = -m_ColumnShift;
                            imgX = m_ColumnShift;
                            break;
                        }
                        case TextAlignment.Right: {
                            textX = m_ColumnShift;
                            imgX = -m_ColumnShift;
                            break;
                        }
                    }

                    Text.Root.SetAnchorPos(textX, Axis.X);
                    AltColumn.RectTransform.SetAnchorPos(imgX, Axis.X);
                }

                if (desiredAltState == DesiredColumnState.Reload) {
                    anims.Add(AnimatedElement.Show(AltColumn, 0.2f));
                }

                yield return Routine.Combine(anims);
            }
        }

        public IEnumerator ResetLayout() {
            if (AnimatedElement.IsActive(AltColumn)) {
                yield return Routine.Combine(
                    AnimatedElement.Hide(AltColumn, 0.2f),
                    ClearLines()
                );

                UnloadColumn?.Invoke();
                AltColumn.Alignment = TextAlignment.Center;
                Text.Root.SetAnchorPos(0, Axis.X);
                Text.Alignment = TextAlignment.Center;
            } else if (Text.Alignment != TextAlignment.Center) {
                yield return ClearLines();
                Text.Root.SetAnchorPos(0, Axis.X);
                Text.Alignment = TextAlignment.Center;
            }
        }

        public IEnumerator HandleNodeStart(ScriptNode node, LeafThreadState thread) {
            bool needsClear = node.HasFlags(ScriptNodeFlags.ClearText);
            if (needsClear) {
                yield return ClearLines();
                yield return 0.2f;
            }
        }

        #endregion // Events

        public IEnumerator ClearLines() {
            if (Text.Lines.Count > 0) {
                yield return GameText.AnimateVanish(Text);
                GameText.ClearLines(Text);
            }
        }

        #region ITextDisplayer

        public TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            if (inString.RichText.Length > 0) {
                m_QueuedLine = GameText.AllocLine(Text, Pools);
                StringHash32 characterId = GameText.FindCharacter(inString);
                GameText.PopulateTextLine(m_QueuedLine, inString.RichText, null, default, Assets.Style(characterId), Assets.Char(characterId), Text.MaxTextWidth);
                GameText.AlignTextLine(m_QueuedLine, GameText.ComputeDesiredAlignment(m_QueuedLine, Text));
                GameText.AdjustComputedLocations(Text, 1);
            }

            m_ForceNext = inString.TryFindEvent(GameText.Events.ForceNext, out var _);
            m_AutoContinue = -1;
            return m_Handlers;
        }

        public IEnumerator TypeLine(TagString inSourceString, TagTextData inType) {
            StringHash32 characterId = GameText.FindCharacter(inSourceString);
            if ((m_RequireInputForPlayerText && GameText.IsPlayer(characterId)) || inSourceString.TryFindEvent(GameText.Events.ForceInput, out var _)) {
                yield return GameText.WaitForPlayerNext(Choices, inSourceString.RichText, Assets.Style(characterId), GetDefaultChoiceAnchor());
            }

            yield return GameText.AnimateLocations(Text, 1);
            GameText.ClearOverflowLines(Text);
            yield return 0.3f;
        }

        public IEnumerator CompleteLine() {
            if (!m_ForceNext) {
                bool hasChoices = LookupNextChoice();
                if (hasChoices) {
                    yield break;
                }

                var nextLineText = LookupNextLine();
                if (nextLineText != null) {
                    StringHash32 characterId = GameText.FindCharacter(nextLineText);
                    if ((m_RequireInputForPlayerText && GameText.IsPlayer(characterId)) || nextLineText.TryFindEvent(GameText.Events.ForceInput, out var _)) {
                        yield break;
                    }
                }
            }

            if (m_AutoContinue >= 0) {
                yield return m_AutoContinue;
            } else {
                yield return GameText.WaitForDefaultNext(Choices, Assets.Style("action"), GetDefaultChoiceAnchor(), true);
            }
        }

        #endregion // ITextDisplayer

        #region IChoiceDisplayer

        public IEnumerator ShowChoice(LeafChoice inChoice, LeafThreadState inThread, ILeafPlugin inPlugin) {
            using(PooledList<LeafChoice.Option> fullOptions = PooledList<LeafChoice.Option>.Create()) {
                fullOptions.AddRange(inChoice.AvailableOptions(ScriptSystem.ChoicePredicate));
                bool hasTime = false;
                foreach(var option in fullOptions) {
                    if (inChoice.HasCustomData(option.TargetId, GameText.ChoiceData.Time)) {
                        hasTime = true;
                        break;
                    }
                }

                int over = Math.Max(0, fullOptions.Count - Choices.MaxOptions);
                if (over > 0) {
                    Log.Error("[TextDisplaySystem] More than {0} options provided - trimming last {1} options", Choices.MaxOptions, over);
                    fullOptions.RemoveRange(fullOptions.Count - over, over);
                }

                if (fullOptions.Count == 1 && !hasTime) {
                    var choices = inChoice.AvailableOptions().GetEnumerator();
                    choices.MoveNext();
                    var choice = choices.Current;

                    TagString choiceText = LookupLine(choice.LineCode);
                    StringHash32 characterId = GameText.FindCharacter(choiceText);
                    if (characterId.IsEmpty) {
                        characterId = GameText.Characters.Action;
                    }
                    yield return GameText.WaitForPlayerNext(Choices, choiceText.RichText, Assets.Style(characterId), GetDefaultChoiceAnchor());
                    inChoice.Choose(choice.TargetId);
                } else {
                    // init option locations for map
                    int numOptions = fullOptions.Count;
                    StringHash32[] locIds = new StringHash32[numOptions];
                    int optionIndex = 0;
                    MapMarkerLoader.OpenMarkerStream(this.gameObject);
                    StringHash32 currentLocation = Player.Location();

                    foreach(var option in fullOptions) {
                        TextChoice choice = GameText.AllocChoice(Choices, Pools);
                        float timeCost = inChoice.GetCustomData(option.Index, GameText.ChoiceData.Time).AsFloat();
                        TagString choiceText = LookupLine(option.LineCode);
                        StringHash32 characterId = GameText.FindCharacter(choiceText);
                        if (characterId.IsEmpty) {
                            characterId = GameText.Characters.Action;
                        }

                        // save option locations for map
                        StringHash32 locId = inChoice.GetCustomData(option.Index, GameText.ChoiceData.LocationId).AsStringHash();
                        MapMarker iconMarker = null;
                        if (!locId.IsEmpty) {
                            locIds[optionIndex] = locId;
                            optionIndex++;
                            iconMarker = MapMarkerLoader.StreamIn(locId, this.gameObject);
                        }

                        GameText.PopulateChoice(choice, choiceText.RichText, option.TargetId, timeCost, iconMarker, Assets.Style(characterId));
                    }
                    // send option locations to map
                    Game.Events.Dispatch(GameEvents.ChoiceOptionsUpdating, locIds);
                    Game.Events.Dispatch(GameEvents.ChoiceOptionsUpdated);

                    MapMarkerLoader.CloseMarkerStream(this.gameObject);

                    GameText.RecomputeAllLocations(Choices);

                    yield return Routine.Combine(
                        Text.Root.AnchorPosTo(Text.RootBaseline + m_ChoiceRowsOffset, m_ChoiceRowsAnim, Axis.Y),
                        AltColumn?.RectTransform.AnchorPosTo(m_AltColumnBaseline + m_ChoiceRowsOffset, m_ChoiceRowsAnim, Axis.Y).DelayBy(0.03f)
                    );

                    yield return GameText.AnimateLocations(Choices);
                    yield return GameText.WaitForChoice(Choices, inChoice);
                    Game.Events.Dispatch(GameEvents.ChoicesClearing);
                    yield return GameText.AnimateVanish(Choices);
                    GameText.ClearChoices(Choices);

                    yield return Routine.Combine(
                        Text.Root.AnchorPosTo(Text.RootBaseline, m_ChoiceRowsAnim, Axis.Y),
                        AltColumn?.RectTransform.AnchorPosTo(m_AltColumnBaseline, m_ChoiceRowsAnim, Axis.Y).DelayBy(0.03f)
                    );
                    yield return 0.2f;
                }
            }
        }

        private TextAnchor GetDefaultChoiceAnchor() {
            if (!m_AlignDefaultChoiceButtonToText) {
                return TextAnchor.MiddleCenter;
            }

            switch(Text.Alignment) {
                case TextAlignment.Left: {
                    return TextAnchor.MiddleLeft;
                }
                case TextAlignment.Right: {
                    return TextAnchor.MiddleRight;
                }
                default: {
                    return TextAnchor.MiddleCenter;
                }
            }
        }

        private bool? GetAllowFullscreenInput() {
            if (m_RequireInputForPlayerText) {
                return false;
            }

            return null;
        }

        #endregion // IChoiceDisplayer

        public void ClearAll() {
            GameText.ClearChoices(Choices);
            GameText.ClearLines(Text);
            m_QueuedLine = null;

            UnloadColumn?.Invoke();
            AnimatedElement.Hide(AltColumn);
            AltColumn.Alignment = TextAlignment.Center;

            Text.Root.SetAnchorPos(0, Axis.X);
            Text.ListRoot.SetAnchorPos(Text.RootBaseline, Axis.Y);
            Text.Alignment = TextAlignment.Center;
        }

        public IEnumerator ClearAllAnimated() {
            if (Choices.Choices.Count > 0) {
                yield return GameText.AnimateVanish(Choices);
                GameText.ClearChoices(Choices);
            }
            yield return ResetLayout();
            yield return ClearLines();
        }
    }
}