using UnityEngine;
using Leaf.Defaults;
using Leaf;
using Leaf.Runtime;
using System.Collections;
using BeauUtil.Tags;
using BeauUtil;
using BeauUtil.Debugger;
using BeauRoutine;
using EasyAssetStreaming;
using BeauPools;
using System;

namespace Journalism {
    public sealed class TextDisplaySystem : MonoBehaviour, ITextDisplayer, IChoiceDisplayer {

        public delegate TagString LookupLineDelegate(StringHash32 id);
        public delegate TagString NextLineDelegate();
        public delegate bool NextChoicesDelegate();

        #region Inspector

        [SerializeField] private TextPools m_Pools = null;
        [SerializeField] private TextLineScroll m_TextDisplay = null;
        [SerializeField] private TextChoiceGroup m_ChoiceDisplay = null;

        [Header("Animation")]
        [SerializeField] private float m_ChoiceRowsOffset = 48;
        [SerializeField] private TweenSettings m_ChoiceRowsAnim = new TweenSettings(0.2f, Curve.Smooth);
        [SerializeField] private float m_StatsOffset = 32;

        [Header("Image Contents")]
        [SerializeField] private ImageColumn m_Image = null;
        [SerializeField] private float m_ColumnShift = 250;

        #endregion // Inspector

        private TextLine m_QueuedLine;
        [NonSerialized] private TextAlignment m_ImagePosition = TextAlignment.Center;

        public LookupLineDelegate LookupLine;
        public NextLineDelegate LookupNextLine;
        public NextChoicesDelegate LookupNextChoice;

        #region Unity

        private void Awake() {
            GameText.InitializeScroll(m_TextDisplay);
            GameText.InitializeChoices(m_ChoiceDisplay);

            Game.Events.Register<StringHash32>(Events.InventoryUpdated, OnInventoryUpdated, this)
                .Register<int[]>(Events.StatsUpdated, OnStatsUpdated, this);
        }

        #endregion // Unity

        #region Events

        public void ConfigureHandlers(CustomTagParserConfig config, TagStringEventHandler handlers) {
            handlers.Register(GameText.Events.Character, HandleCharacter)
                .Register(GameText.Events.Image, HandleImage)
                .Register(GameText.Events.ClearImage, HandleImageClear);
        }

        private void HandleCharacter(TagEventData evtData, object context) {
            StringHash32 characterId = evtData.GetStringHash();
            // TODO: Some indirection? Character -> Style as opposed to Character == Style?
            SetStyle(characterId);
        }

        private IEnumerator HandleImage(TagEventData evtData, object context) {
            var args = evtData.ExtractStringArgs();
            StringSlice path = args[0];

            if (path.IsEmpty) {
                yield return HandleImageClear(evtData, context);
                yield break;
            }

            TextAlignment align = m_ImagePosition;
            if (args.Count > 1) {
                align = StringParser.ConvertTo<TextAlignment>(args[1], m_ImagePosition);
            }
            if (align == TextAlignment.Center) {
                align = TextAlignment.Left;
            }

            // if everything is aligned, no need to do anything more.
            bool needsRealign = align != m_ImagePosition;
            bool needsFadeOut = m_Image.Root.gameObject.activeSelf;
            bool needsChangeTexture = path != m_Image.Texture.Path;

            if (!needsRealign && !needsFadeOut && !needsChangeTexture) {
                yield break;
            }

            m_ImagePosition = align;

            using(PooledList<IEnumerator> anims = PooledList<IEnumerator>.Create()) {
                if (needsRealign) {
                    anims.Add(ClearLines());
                }
                if (needsFadeOut) {
                    anims.Add(m_Image.TextureGroup.FadeTo(0, 0.3f));
                }

                yield return Routine.Combine(anims);
                anims.Clear();

                if (needsChangeTexture) {
                    m_Image.Texture.Path = path.ToString();
                    m_Image.Texture.Preload();
                    while(m_Image.Texture.IsLoading()) {
                        yield return null;
                    }
                }

                if (needsRealign) {
                    float imgX = 0, textX = 0;
                    switch(m_ImagePosition) {
                        case TextAlignment.Left: {
                            imgX = -m_ColumnShift;
                            textX = m_ColumnShift;
                            break;
                        }
                        case TextAlignment.Right: {
                            imgX = m_ColumnShift;
                            textX = -m_ColumnShift;
                            break;
                        }
                    }

                    m_TextDisplay.Root.SetAnchorPos(textX, Axis.X);
                    m_Image.Root.SetAnchorPos(imgX, Axis.X);
                    m_TextDisplay.Alignment = TextAlignment.Left;
                }

                m_Image.TextureGroup.alpha = 0;
                m_Image.Root.gameObject.SetActive(true);
                anims.Add(m_Image.TextureGroup.FadeTo(1, 0.3f));

                yield return Routine.Combine(anims);
            }
        }
        
        private IEnumerator HandleImageClear(TagEventData evtData, object context) {
            if (m_ImagePosition == TextAlignment.Center) {
                yield break;
            }

            if (m_Image.Root.gameObject.activeSelf) {
                yield return Routine.Combine(
                    m_Image.TextureGroup.FadeTo(0, 0.3f),
                    ClearLines()
                );

                m_Image.Texture.Unload();
                m_Image.Root.gameObject.SetActive(false);
                m_TextDisplay.Root.SetAnchorPos(0, Axis.X);
            }

            m_TextDisplay.Alignment = TextAlignment.Center;
            m_ImagePosition = TextAlignment.Center;

            Streaming.UnloadUnusedAsync();
        }

        private void SetStyle(StringHash32 styleId) {
            var style = Assets.Style(styleId);
            Assert.NotNull(m_QueuedLine);
            GameText.SetTextLineStyle(m_QueuedLine, style);
        }

        public IEnumerator HandleNodeStart(ScriptNode node, LeafThreadState thread) {
            bool needsClear = node.HasFlags(ScriptNodeFlags.ClearText);
            if (needsClear) {
                yield return ClearLines();
                yield return 0.2f;
            }
        }

        private void OnInventoryUpdated(StringHash32 scrapId) {
            if (scrapId.IsEmpty) {
                return;
            }

            Game.Scripting.Interrupt(DisplayNewStoryScrap(scrapId));
        }

        private IEnumerator DisplayNewStoryScrap(StringHash32 scrapId) {
            // TODO: Localization
            TextLine line = GameText.AllocLine(m_TextDisplay, m_Pools);
            GameText.PopulateTextLine(line, "New Story Snippet!", null, default, Assets.Style("msg"));
            GameText.AlignTextLine(line, TextAlignment.Center);
            GameText.AdjustComputedLocations(m_TextDisplay, 1);
            yield return GameText.AnimateLocations(m_TextDisplay, 1);
            yield return 0.1f;

            StoryScrapData data = Assets.Scrap(scrapId);
            StoryScrapDisplay scrap = GameText.AllocScrap(data, m_TextDisplay, m_Pools);
            GameText.PopulateStoryScrap(scrap, data, Assets.DefaultStyle);
            GameText.AlignTextLine(scrap.Line, TextAlignment.Center);
            GameText.AdjustComputedLocations(m_TextDisplay, 1);
            yield return GameText.AnimateLocations(m_TextDisplay, 1);

            yield return GameText.WaitForDefaultNext(m_ChoiceDisplay, Assets.Style("action"));
        }

        private void OnStatsUpdated(int[] adjustments) {
            Game.Scripting.Interrupt(DisplayStatsAdjusted(adjustments));
        }

        private IEnumerator DisplayStatsAdjusted(int[] adjustments) {
            // TODO: Implement for real
            using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                for(int i = 0; i < Stats.Count; i++) {
                    int adjust = adjustments[i];
                    if (adjust != 0) {
                        if (psb.Builder.Length > 0) {
                            psb.Builder.Append(" ");
                        }
                        string name = Stats.Name((StatId) i);
                        if (adjust > 0) {
                            psb.Builder.Append("<b>").Append(name).Append("</b>").Append('+', Mathf.CeilToInt(adjust / 4f));
                        } else {
                            psb.Builder.Append("<b>").Append(name).Append("</b>").Append('-', Mathf.CeilToInt(-adjust / 4f));
                        }
                    }
                }

                TextLine line = GameText.AllocLine(m_TextDisplay, m_Pools);
                GameText.PopulateTextLine(line, psb.ToString(), null, default, Assets.Style("msg"));
                GameText.AlignTextLine(line, TextAlignment.Center);
                GameText.AdjustComputedLocations(m_TextDisplay, 1);
                yield return GameText.AnimateLocations(m_TextDisplay, 1);
                yield return 0.1f;

                yield return GameText.WaitForDefaultNext(m_ChoiceDisplay, Assets.Style("action"));
            }
        }

        #endregion // Events

        public IEnumerator ClearLines() {
            if (m_TextDisplay.Lines.Count > 0) {
                yield return GameText.AnimateVanish(m_TextDisplay);
                GameText.ClearLines(m_TextDisplay);
            }
        }

        #region ITextDisplayer

        public TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            if (inString.RichText.Length > 0) {
                m_QueuedLine = GameText.AllocLine(m_TextDisplay, m_Pools);
                StringHash32 characterId = GameText.FindCharacter(inString);
                GameText.PopulateTextLine(m_QueuedLine, inString.RichText, null, default, Assets.Style(characterId));
                GameText.AlignTextLine(m_QueuedLine, GameText.ComputeDesiredAlignment(m_QueuedLine, m_TextDisplay));
                GameText.AdjustComputedLocations(m_TextDisplay, 1);
            }

            return null;
        }

        public IEnumerator TypeLine(TagString inSourceString, TagTextData inType) {
            yield return GameText.AnimateLocations(m_TextDisplay, 1);
            GameText.ClearOverflowLines(m_TextDisplay);
            yield return 0.5f;
        }

        public IEnumerator CompleteLine() {
            bool hasChoices = LookupNextChoice();
            if (hasChoices) {
                yield return 0.5f;
                yield break;
            }

            var nextLineText = LookupNextLine();
            if (nextLineText != null) {
                StringHash32 characterId = GameText.FindCharacter(nextLineText);
                if (GameText.IsPlayer(characterId)) {
                    yield return GameText.WaitForPlayerNext(m_ChoiceDisplay, nextLineText.RichText, Assets.Style(characterId));
                    yield break;
                }
            }

            yield return GameText.WaitForDefaultNext(m_ChoiceDisplay, Assets.Style("action"));
        }

        #endregion // ITextDisplayer

        #region IChoiceDisplayer

        public IEnumerator ShowChoice(LeafChoice inChoice, LeafThreadState inThread, ILeafPlugin inPlugin) {
            using(PooledList<LeafChoice.Option> fullOptions = PooledList<LeafChoice.Option>.Create()) {
                fullOptions.AddRange(inChoice.AvailableOptions(ChoicePredicate));
                bool hasTime = false;
                foreach(var option in fullOptions) {
                    if (inChoice.HasCustomData(option.TargetId, GameText.ChoiceData.Time)) {
                        hasTime = true;
                        break;
                    }
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
                    yield return GameText.WaitForPlayerNext(m_ChoiceDisplay, choiceText.RichText, Assets.Style(characterId));
                    inChoice.Choose(choice.TargetId);
                } else {
                    foreach(var option in fullOptions) {
                        TextChoice choice = GameText.AllocChoice(m_ChoiceDisplay, m_Pools);
                        uint timeCost = Stats.HoursToTimeUnits(inChoice.GetCustomData(option.Index, GameText.ChoiceData.Time).AsFloat());
                        TagString choiceText = LookupLine(option.LineCode);
                        StringHash32 characterId = GameText.FindCharacter(choiceText);
                        if (characterId.IsEmpty) {
                            characterId = GameText.Characters.Action;
                        }
                        GameText.PopulateChoice(choice, choiceText.RichText, option.TargetId, timeCost, Assets.Style(characterId));
                    }

                    GameText.RecomputeAllLocations(m_ChoiceDisplay);

                    yield return m_TextDisplay.Root.AnchorPosTo(m_TextDisplay.RootBaseline + m_ChoiceRowsOffset, m_ChoiceRowsAnim, Axis.Y);
                    
                    yield return GameText.AnimateLocations(m_ChoiceDisplay);
                    yield return GameText.WaitForChoice(m_ChoiceDisplay, inChoice);
                    yield return GameText.AnimateVanish(m_ChoiceDisplay);
                    GameText.ClearChoices(m_ChoiceDisplay);

                    yield return m_TextDisplay.Root.AnchorPosTo(m_TextDisplay.RootBaseline, m_ChoiceRowsAnim, Axis.Y);
                    yield return 0.2f;
                }
            }
        }

        static private readonly LeafChoice.OptionPredicate ChoicePredicate = (choice, option) => {
            float timeCost = choice.GetCustomData(option.Index, GameText.ChoiceData.Time).AsFloat();
            if (!Player.HasTime(timeCost)) {
                return false;
            }

            bool once = choice.HasCustomData(option.Index, GameText.ChoiceData.Once);
            if (once && Player.Visited(option.TargetId.AsStringHash())) {
                return false;
            }

            return true;
        };

        #endregion // IChoiceDisplayer

        public void ClearAll() {
            GameText.ClearChoices(m_ChoiceDisplay);
            GameText.ClearLines(m_TextDisplay);
            m_QueuedLine = null;

            m_Image.Texture.Unload();
            m_Image.Root.gameObject.SetActive(false);
            m_TextDisplay.Root.SetAnchorPos(0, Axis.X);
            m_TextDisplay.ListRoot.SetAnchorPos(m_TextDisplay.RootBaseline, Axis.Y);

            m_TextDisplay.Alignment = TextAlignment.Center;
            m_ImagePosition = TextAlignment.Center;

            Streaming.UnloadUnusedAsync();
        }
    }

    public enum TextLayoutId {
        Left,
        Center,
        Right
    }
}