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
using Journalism.UI;

namespace Journalism {
    public sealed class TextDisplaySystem : MonoBehaviour, ITextDisplayer, IChoiceDisplayer {

        public delegate TagString LookupLineDelegate(StringHash32 id);
        public delegate TagString NextLineDelegate();
        public delegate bool NextChoicesDelegate();

        #region Inspector

        [SerializeField] private TextPools m_Pools = null;
        [SerializeField] private TextLineScroll m_TextDisplay = null;
        [SerializeField] private TextChoiceGroup m_ChoiceDisplay = null;
        
        [Header("Feedback Sequence")]
        [SerializeField] private NewspaperLayout m_FinishedStoryLayout = null;
        [SerializeField] private StoryQualityDisplay m_StoryQualityLayout = null;
        [SerializeField] private StoryAttributeDisplay m_StoryAttributeLayout = null;
        [SerializeField] private StoryScoreDisplay m_StoryScoreLayout = null;
        [SerializeField] private CanvasGroup m_EditorOverlay = null;

        [Header("Animation")]
        [SerializeField] private float m_ChoiceRowsOffset = 48;
        [SerializeField] private TweenSettings m_ChoiceRowsAnim = new TweenSettings(0.2f, Curve.Smooth);
        [SerializeField] private float m_StatsOffset = 32;

        [Header("Image Contents")]
        [SerializeField] private ImageColumn m_Image = null;
        [SerializeField] private ImageColumn m_Map = null;
        [SerializeField] private float m_ColumnShift = 250;

        #endregion // Inspector

        [NonSerialized] private TextLine m_QueuedLine;
        [NonSerialized] private TextAlignment m_ImagePosition = TextAlignment.Center;
        [NonSerialized] private TextAlignment m_MapPosition = TextAlignment.Center;
        [NonSerialized] private float m_ImageColumnBaseline;
        [NonSerialized] private float m_AutoContinue = -1;
        private Routine m_OverlayAnim;

        public LookupLineDelegate LookupLine;
        public NextLineDelegate LookupNextLine;
        public NextChoicesDelegate LookupNextChoice;

        #region Unity

        private void Awake() {
            GameText.InitializeScroll(m_TextDisplay);
            GameText.InitializeChoices(m_ChoiceDisplay);

            m_ImageColumnBaseline = m_Image.Root.anchoredPosition.y;

            Game.Events.Register<StringHash32>(GameEvents.InventoryUpdated, OnInventoryUpdated, this)
                .Register<int[]>(GameEvents.StatsUpdated, OnStatsUpdated, this)
                .Register(GameEvents.StoryEvalBegin, OnFeedbackBegin, this)
                .Register(GameEvents.StoryEvalEnd, OnFeedbackEnd, this);
        }

        #endregion // Unity

        #region Events

        public void ConfigureHandlers(CustomTagParserConfig config, TagStringEventHandler handlers) {
            handlers.Register(LeafUtils.Events.Character, HandleCharacter)
                .Register(GameText.Events.Image, HandleImage)
                .Register(GameText.Events.ClearImage, HandleImageClear)
                .Register(GameText.Events.Anim, HandleAnim)
                .Register(GameText.Events.Auto, HandleAuto)
                .Register(GameText.Events.DisplayStoryStats, HandleStoryStats)
                .Register(GameText.Events.DisplayStoryScore, HandleStoryScore)
                .Register(GameText.Events.Map, HandleMap)
                .Register(GameText.Events.ClearMap, HandleMapClear);
        }

        private void HandleCharacter(TagEventData evtData, object context) {
            StringHash32 characterId = evtData.GetStringHash();
            // TODO: Some indirection? Character -> Style as opposed to Character == Style?
            SetStyle(characterId);
        }

        private void HandleAnim(TagEventData evtData, object context) {
            if (m_QueuedLine != null) {
                GameText.SetAnim(m_QueuedLine, evtData.StringArgument, ref m_TextDisplay.Lines[0].StyleAnimation);
            }
        }

        private void HandleAuto(TagEventData evtData, object context) {
            m_AutoContinue = evtData.GetFloat();
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
        }

        private IEnumerator HandleMap(TagEventData evtData, object context) {
            Game.Events.Register(GameEvents.ChoiceOptionsUpdated, OnChoiceOptionsUpdated);

            var args = evtData.ExtractStringArgs();
            StringSlice path = args[0];

            TextAlignment align = m_ImagePosition;
            if (args.Count > 1) {
                align = StringParser.ConvertTo<TextAlignment>(args[1], m_ImagePosition);
            }
            if (align == TextAlignment.Center) {
                align = TextAlignment.Left;
            }

            // if everything is aligned, no need to do anything more.
            bool needsRealign = align != m_MapPosition;
            bool needsFadeOut = m_Map.Root.gameObject.activeSelf;
            bool needsChangeTexture = path != m_Map.Texture.Path;

            if (!needsRealign && !needsFadeOut && !needsChangeTexture) {
                yield break;
            }

            m_MapPosition = align;

            using (PooledList<IEnumerator> anims = PooledList<IEnumerator>.Create()) {
                if (needsRealign) {
                    anims.Add(ClearLines());
                }
                if (needsFadeOut) {
                    anims.Add(m_Map.TextureGroup.FadeTo(0, 0.3f));
                }

                yield return Routine.Combine(anims);
                anims.Clear();

                if (needsChangeTexture) {
                    m_Map.Texture.Path = path.ToString();
                    m_Map.Texture.Preload();
                    while (m_Map.Texture.IsLoading()) {
                        yield return null;
                    }

                    // resize texture to match root width
                    Vector2 dimsRatio = new Vector2(1,
                        m_Map.Root.GetComponent<RectTransform>().rect.height
                        / m_Map.Root.GetComponent<RectTransform>().rect.width);

                    RectTransform mapTexRect = m_Map.Texture.GetComponent<RectTransform>();
                    mapTexRect.sizeDelta =
                        new Vector2(mapTexRect.rect.width * dimsRatio.x, mapTexRect.rect.width * dimsRatio.y);
                }

                if (needsRealign) {
                    float imgX = 0, textX = 0;
                    switch (m_MapPosition) {
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
                    m_Map.Root.SetAnchorPos(imgX, Axis.X);
                    m_TextDisplay.Alignment = TextAlignment.Left;
                }

                m_Map.TextureGroup.alpha = 0;
                m_Map.Root.gameObject.SetActive(true);
                anims.Add(m_Map.TextureGroup.FadeTo(1, 0.3f));

                yield return Routine.Combine(anims);
            }
        }

        private IEnumerator HandleMapClear(TagEventData evtData, object context) {

            if (m_MapPosition == TextAlignment.Center) {
                yield break;
            }

            if (m_Map.Root.gameObject.activeSelf) {
                yield return Routine.Combine(
                    m_Map.TextureGroup.FadeTo(0, 0.3f),
                    ClearLines()
                );

                m_Map.Texture.Unload();
                m_Map.Root.gameObject.SetActive(false);
                m_TextDisplay.Root.SetAnchorPos(0, Axis.X);
            }

            m_TextDisplay.Alignment = TextAlignment.Center;
            m_MapPosition = TextAlignment.Center;

            // Clear markers from the map
            MapMarkerLoader.ClearMarkerContainer(m_Map.gameObject);

            Game.Events.Deregister(GameEvents.ChoiceOptionsUpdated, OnChoiceOptionsUpdated);
        }

        private IEnumerator HandleStoryStats(TagEventData evtData, object context) {
            // Attribute Distribution
            m_StoryAttributeLayout.gameObject.SetActive(true);
            GameText.PopulateStoryAttributeDistribution(m_StoryAttributeLayout.Target, Assets.CurrentLevel.Story);
            GameText.PopulateStoryAttributeDistribution(m_StoryAttributeLayout.Current, Player.StoryStatistics);
            GameText.InsertTextLine(m_TextDisplay, m_StoryAttributeLayout.Line, HandleFreeStoryStat);
            GameText.AlignTextLine(m_StoryAttributeLayout.Line, TextAlignment.Center);
            GameText.AdjustComputedLocations(m_TextDisplay, 1);
            yield return GameText.AnimateLocations(m_TextDisplay, 1);
            GameText.ClearOverflowLines(m_TextDisplay);
            yield return 0.2f;

            // Quality
            m_StoryQualityLayout.gameObject.SetActive(true);
            GameText.PopulateStoryQuality(m_StoryQualityLayout, Player.StoryStatistics);
            GameText.InsertTextLine(m_TextDisplay, m_StoryQualityLayout.Line, HandleFreeStoryStat);
            GameText.AlignTextLine(m_StoryQualityLayout.Line, TextAlignment.Center);
            GameText.AdjustComputedLocations(m_TextDisplay, 1);
            yield return GameText.AnimateLocations(m_TextDisplay, 1);
            GameText.ClearOverflowLines(m_TextDisplay);
            yield return 0.2f;

            yield return CompleteLine();
        }

        private IEnumerator HandleStoryScore(TagEventData evtData, object context) {
            // TODO: Make this much better
            m_StoryScoreLayout.gameObject.SetActive(true);
            GameText.InsertTextLine(m_TextDisplay, m_StoryScoreLayout.Line, HandleFreeStoryStat);
            switch(Player.StoryStatistics.Score) {
                case StoryScore.Bad: {
                    GameText.PopulateTextLine(m_StoryScoreLayout.Line, "I know you're just a rookie, but I expected more from someone who went to a fancy school.", null, default, null);
                    break;
                }

                case StoryScore.Medium: {
                    GameText.PopulateTextLine(m_StoryScoreLayout.Line, "Not too shabby, but it's not winning any awards.", null, default, null);
                    break;
                }

                case StoryScore.Good: {
                    GameText.PopulateTextLine(m_StoryScoreLayout.Line, "This story is fantastic. You're going places kid.", null, default, null);
                    break;
                }
            }
            GameText.AlignTextLine(m_StoryScoreLayout.Line, TextAlignment.Center);
            GameText.AdjustComputedLocations(m_TextDisplay, 1);
            yield return GameText.AnimateLocations(m_TextDisplay, 1);
            GameText.ClearOverflowLines(m_TextDisplay);
            yield return 0.2f;

            yield return CompleteLine();
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

        private void OnFeedbackBegin() {
            m_OverlayAnim.Replace(this, m_EditorOverlay.Show(0.2f));
        }

        private void OnFeedbackEnd() {
            m_OverlayAnim.Replace(this, m_EditorOverlay.Hide(0.2f));
        }

        private IEnumerator DisplayNewStoryScrap(StringHash32 scrapId) {
            // TODO: Localization
            TextLine line = GameText.AllocLine(m_TextDisplay, m_Pools);
            GameText.PopulateTextLine(line, "New Story Snippet!", null, default, Assets.Style("msg"));
            GameText.AlignTextLine(line, TextAlignment.Center);
            GameText.AdjustComputedLocations(m_TextDisplay, 1);
            yield return GameText.AnimateLocations(m_TextDisplay, 1);
            GameText.ClearOverflowLines(m_TextDisplay);
            yield return 0.1f;

            StoryScrapData data = Assets.Scrap(scrapId);
            if (data != null) {
                StoryScrapDisplay scrap = GameText.AllocScrap(data, m_TextDisplay, m_Pools);
                GameText.PopulateStoryScrap(scrap, data, Assets.Style("snippet"));
                GameText.AlignTextLine(scrap.Line, TextAlignment.Center);
                GameText.AdjustComputedLocations(m_TextDisplay, 1);
                yield return GameText.AnimateLocations(m_TextDisplay, 1);
            } else {
                TextLine scrap = GameText.AllocLine(m_TextDisplay, m_Pools);
                GameText.PopulateTextLine(scrap, "ERROR: No scrap with id " + scrapId.ToDebugString(), null, default, Assets.Style("error"));
                GameText.AlignTextLine(scrap, TextAlignment.Center);
                GameText.AdjustComputedLocations(m_TextDisplay, 1);
                yield return GameText.AnimateLocations(m_TextDisplay, 1);
            }

            GameText.ClearOverflowLines(m_TextDisplay);

            yield return CompleteLine();
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
                GameText.ClearOverflowLines(m_TextDisplay);
                yield return 0.1f;

                yield return CompleteLine();
            }
        }

        private void HandleFreeStoryStat(TextLine line) {
            line.gameObject.SetActive(false);
            line.transform.SetParent(m_Pools.LinePool.PoolTransform);
        }

        private void OnChoiceOptionsUpdated() {
            // Add markers to the map
            MapMarkerLoader.PopulateMapWithMarkers(m_Map.Texture, m_Map.gameObject);
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

            m_AutoContinue = -1;

            return null;
        }

        public IEnumerator TypeLine(TagString inSourceString, TagTextData inType) {
            StringHash32 characterId = GameText.FindCharacter(inSourceString);
            if (GameText.IsPlayer(characterId) || inSourceString.TryFindEvent(GameText.Events.ForceInput, out var _)) {
                yield return GameText.WaitForPlayerNext(m_ChoiceDisplay, inSourceString.RichText, Assets.Style(characterId));
            }

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
                if (GameText.IsPlayer(characterId) || nextLineText.TryFindEvent(GameText.Events.ForceInput, out var _)) {
                    yield break;
                }
            }

            if (m_AutoContinue >= 0) {
                yield return m_AutoContinue;
            } else {
                yield return GameText.WaitForDefaultNext(m_ChoiceDisplay, Assets.Style("action"));
            }
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

                int over = Math.Max(0, fullOptions.Count - m_ChoiceDisplay.MaxOptions);
                if (over > 0) {
                    Log.Error("[TextDisplaySystem] More than {0} options provided - trimming last {1} options", m_ChoiceDisplay.MaxOptions, over);
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
                    yield return GameText.WaitForPlayerNext(m_ChoiceDisplay, choiceText.RichText, Assets.Style(characterId));
                    inChoice.Choose(choice.TargetId);
                } else {
                    // init option locations for map
                    int numOptions = fullOptions.Count;
                    StringHash32[] locIds = new StringHash32[numOptions];
                    int optionIndex = 0;
                    MapMarkerLoader.OpenMarkerStream(this.gameObject);
                    StringHash32 currentLocation = Player.Location();

                    foreach(var option in fullOptions) {
                        TextChoice choice = GameText.AllocChoice(m_ChoiceDisplay, m_Pools);
                        uint timeCost = Stats.HoursToTimeUnits(inChoice.GetCustomData(option.Index, GameText.ChoiceData.Time).AsFloat());
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
                    Game.Events.Dispatch(GameEvents.ChoiceOptionsUpdated, locIds);

                    MapMarkerLoader.CloseMarkerStream(this.gameObject);

                    GameText.RecomputeAllLocations(m_ChoiceDisplay);

                    yield return Routine.Combine(
                        m_TextDisplay.Root.AnchorPosTo(m_TextDisplay.RootBaseline + m_ChoiceRowsOffset, m_ChoiceRowsAnim, Axis.Y),
                        m_Image.Root.AnchorPosTo(m_ImageColumnBaseline + m_ChoiceRowsOffset, m_ChoiceRowsAnim, Axis.Y).DelayBy(0.03f)
                    );
                    
                    yield return GameText.AnimateLocations(m_ChoiceDisplay);
                    yield return GameText.WaitForChoice(m_ChoiceDisplay, inChoice);
                    yield return GameText.AnimateVanish(m_ChoiceDisplay);
                    GameText.ClearChoices(m_ChoiceDisplay);

                    yield return Routine.Combine(
                        m_TextDisplay.Root.AnchorPosTo(m_TextDisplay.RootBaseline, m_ChoiceRowsAnim, Axis.Y),
                        m_Image.Root.AnchorPosTo(m_ImageColumnBaseline, m_ChoiceRowsAnim, Axis.Y).DelayBy(0.03f)
                    );
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

            m_FinishedStoryLayout.gameObject.SetActive(false);
            m_EditorOverlay.Hide();
            m_OverlayAnim.Stop();

            m_TextDisplay.Alignment = TextAlignment.Center;
            m_ImagePosition = TextAlignment.Center;
        }

        public IEnumerator ClearAllAnimated() {
            if (m_ChoiceDisplay.Choices.Count > 0) {
                yield return GameText.AnimateVanish(m_ChoiceDisplay);
                GameText.ClearChoices(m_ChoiceDisplay);
            }
            yield return HandleImageClear(default, null);
            yield return ClearLines();
        }

        public IEnumerator DisplayNewspaper() {
            UISystem.SetHeaderEnabled(false);
            yield return ClearAllAnimated();

            m_FinishedStoryLayout.gameObject.SetActive(true);
            m_FinishedStoryLayout.Root.SetAnchorPos(-660, Axis.Y);
            m_FinishedStoryLayout.Root.SetRotation(RNG.Instance.NextFloat(-1, 1), Axis.Z, Space.Self);
            yield return null;
            StoryText.LayoutNewspaper(m_FinishedStoryLayout, Assets.CurrentLevel.Story, Player.Data);
            yield return m_FinishedStoryLayout.Root.AnchorPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            yield return 1;
            yield return GameText.WaitForPlayerNext(m_ChoiceDisplay, "Talk to Editor", Assets.Style(GameText.Characters.Action), TextAnchor.LowerRight);
            yield return m_FinishedStoryLayout.Root.AnchorPosTo(660, 0.5f, Axis.Y).Ease(Curve.BackIn);
            m_FinishedStoryLayout.gameObject.SetActive(false);
        }
    }

    public enum TextLayoutId {
        Left,
        Center,
        Right
    }
}