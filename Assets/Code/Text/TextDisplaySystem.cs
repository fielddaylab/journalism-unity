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
using FDLocalization;
using UnityEngine.UI;

namespace Journalism {
    public sealed class TextDisplaySystem : MonoBehaviour, ITextDisplayer, IChoiceDisplayer {
        #region Inspector

        [SerializeField] private TextDisplayLayer m_BaseLayer = null;
        [SerializeField] private TextDisplayLayer m_OverLayer = null;
        
        [Header("Feedback Sequence")]
        [SerializeField] private NewspaperLayout m_FinishedStoryLayout = null;
        [SerializeField] private StoryQualityDisplay m_StoryQualityLayout = null;
        [SerializeField] private StoryAttributeDisplay m_StoryAttributeLayout = null;
        [SerializeField] private ImpactLayout m_ImpactLayout = null;
        [SerializeField] private AnimatedElement m_FeedbackOverlay = null;

        [Header("Animation")]
        [SerializeField] private float m_FeedbackOverlayEditorY = -170f;
        [SerializeField] private float m_FeedbackOverlayImpactY = -75f;

        [Header("Image Contents")]
        [SerializeField] private AnimatedElement m_ImageLayout = null;
        [SerializeField] private ImageColumn m_Image = null;
        [SerializeField] private Image m_ImageBG = null;
        [SerializeField] private ImageColumn m_Map = null;
        [SerializeField] private Image m_Border = null;
        [SerializeField] private Sprite m_ImageBGSprite = null;
        [SerializeField] private Sprite m_PortraitBGSprite = null;

        [Header("Default Dimensions")]
        [SerializeField] private Vector2 m_DefaultImageDims = new Vector2(400, 400);
        [SerializeField] private Vector2 m_DefaultPortraitDims = new Vector2(320, 320);
        [SerializeField] private float m_DefaultPortraitYOffset = -25;
        [SerializeField] private Vector2 m_DefaultMapDims = new Vector2(366, 245);

        #endregion // Inspector

        [NonSerialized] private TextDisplayLayer m_CurrentLayer;

        private Routine m_OverlayAnim;

        public TextDisplayLayer CurrentLayer {
            get { return m_CurrentLayer; }
        }

        #region Unity

        private void Awake() {
            Game.Events.Register<StringHash32>(GameEvents.InventoryUpdated, OnInventoryUpdated, this)
                .Register<int[]>(GameEvents.StatsUpdated, OnStatsUpdated, this)
                .Register(GameEvents.StoryEvalBegin, OnFeedbackBegin, this)
                .Register(GameEvents.StoryEvalEditor, OnFeedbackSwapToEditor, this)
                .Register(GameEvents.StoryEvalImpact, OnFeedbackSwapToImpact, this)
                .Register(GameEvents.StoryEvalEnd, OnFeedbackEnd, this)
                .Register(GameEvents.ChoiceOptionsUpdated, OnChoiceOptionsUpdated, this)
                .Register(GameEvents.ChoicesClearing, OnChoicesClearing, this)
                .Register(GameEvents.TutorialBegin, OnTutorialBegin, this)
                .Register(GameEvents.TutorialEnd, OnTutorialEnd, this);

            m_CurrentLayer = m_BaseLayer;
            
            m_BaseLayer.NeedReloadColumn = CheckColumnLoad;
            m_BaseLayer.LoadColumn = LoadColumnData;
            m_BaseLayer.UnloadColumn = UnloadColumnData;
        }

        #endregion // Unity

        public void HookIntegration(LeafIntegration integration) {
            m_BaseLayer.LookupLine = m_OverLayer.LookupLine = integration.LookupLine;
            m_BaseLayer.LookupNextChoice = m_OverLayer.LookupNextChoice = integration.PredictChoice;
            m_BaseLayer.LookupNextLine = m_OverLayer.LookupNextLine = integration.PredictNextLine;
        }

        #region Events

        public void ConfigureHandlers(CustomTagParserConfig config, TagStringEventHandler handlers) {
            handlers
                .Register(GameText.Events.Image, HandleImage)
                .Register(GameText.Events.ClearImage, HandleImageOrMapClear)
                .Register(GameText.Events.Portrait, HandlePortrait)
                .Register(GameText.Events.ClearPortrait, HandleImageOrMapClear)
                .Register(GameText.Events.DisplayStoryStats, HandleStoryStats)
                .Register(GameText.Events.Map, HandleMap)
                .Register(GameText.Events.ClearMap, HandleImageOrMapClear);

            m_BaseLayer.ConfigureHandlers();
            m_OverLayer.ConfigureHandlers();
        }

        private IEnumerator HandleImage(TagEventData evtData, object context) {
            this.m_BaseLayer.AltColumn.RectTransform.SetSizeDelta(m_DefaultImageDims);
            this.m_ImageBG.rectTransform.SetSizeDelta(m_DefaultImageDims);
            this.m_ImageLayout.RectTransform.SetPosition(0, Axis.Y, Space.Self);
            this.m_Image.transform.SetPosition(0, Axis.Y, Space.Self);
            this.m_Border.gameObject.SetActive(false); // disable border
            this.m_ImageBG.sprite = m_ImageBGSprite;

            yield return HandleImageOrMap(evtData, context);
        }

        private IEnumerator HandlePortrait(TagEventData evtData, object context) {
            this.m_BaseLayer.AltColumn.RectTransform.SetSizeDelta(m_DefaultPortraitDims);
            this.m_ImageBG.rectTransform.SetSizeDelta(m_DefaultPortraitDims);
            this.m_Image.transform.SetPosition(m_DefaultPortraitYOffset, Axis.Y, Space.Self);
            this.m_Border.gameObject.SetActive(true); // enable border
            this.m_ImageBG.sprite = m_PortraitBGSprite;

            yield return HandleImageOrMap(evtData, context);
        }

        private IEnumerator HandleMap(TagEventData evtData, object context) {
            this.m_BaseLayer.AltColumn.RectTransform.SetSizeDelta(m_DefaultMapDims);
            this.m_Border.gameObject.SetActive(true); // enable border

            yield return HandleImageOrMap(evtData, context);
        }

        private IEnumerator HandleImageOrMap(TagEventData evtData, object context) {
            var args = evtData.ExtractStringArgs();
            StringSlice path = args[0];

            TextAlignment? align = m_BaseLayer.Text.Alignment;
            if (args.Count > 1) {
                align = StringParser.ConvertTo<TextAlignment>(args[1], m_CurrentLayer.AltColumn.Alignment).Mirror();
            } else {
                align = TextAlignment.Right;
            }

            return m_CurrentLayer.ShiftLayout(align, evtData, path);
        }
        
        private IEnumerator HandleImageOrMapClear(TagEventData evtData, object context) {
            if (evtData.Type == GameText.Events.ClearMap && m_Map.gameObject.activeSelf) {
                return m_CurrentLayer.ResetLayout();
            } else if (evtData.Type == GameText.Events.ClearImage && m_Image.gameObject.activeSelf) {
                return m_CurrentLayer.ResetLayout();
            } else {
                return null;
            }
        }

        private IEnumerator HandleStoryStats(TagEventData evtData, object context) {
            // Attribute Distribution
            m_StoryAttributeLayout.gameObject.SetActive(true);
            GameText.PopulateStoryAttributeDistribution(m_StoryAttributeLayout.Target, Assets.CurrentLevel.Story);
            GameText.PopulateStoryAttributeDistribution(m_StoryAttributeLayout.Current, Player.StoryStatistics);
            GameText.InsertTextLine(m_CurrentLayer.Text, m_StoryAttributeLayout.Line, HandleFreeStoryStat);
            GameText.AlignTextLine(m_StoryAttributeLayout.Line, TextAlignment.Center);
            GameText.AdjustComputedLocations(m_CurrentLayer.Text, 1);
            yield return GameText.AnimateLocations(m_CurrentLayer.Text, 1);
            GameText.ClearOverflowLines(m_CurrentLayer.Text);
            yield return 0.2f;

            // Quality
            m_StoryQualityLayout.gameObject.SetActive(true);
            GameText.PopulateStoryQuality(m_StoryQualityLayout, Player.StoryStatistics);
            GameText.InsertTextLine(m_CurrentLayer.Text, m_StoryQualityLayout.Line, HandleFreeStoryStat);
            GameText.AlignTextLine(m_StoryQualityLayout.Line, TextAlignment.Center);
            GameText.AdjustComputedLocations(m_CurrentLayer.Text, 1);
            yield return GameText.AnimateLocations(m_CurrentLayer.Text, 1);
            GameText.ClearOverflowLines(m_CurrentLayer.Text);
            yield return 0.2f;

            yield return m_CurrentLayer.CompleteLine();
        }

        private void OnInventoryUpdated(StringHash32 scrapId) {
            if (scrapId.IsEmpty) {
                return;
            }

            if (DebugService.AutoTesting) {
                return;
            }

            Game.Scripting.Interrupt(DisplayNewStoryScrap(scrapId));
        }

        private void OnFeedbackBegin() {
            m_FeedbackOverlay.RectTransform.SetAnchorPos(m_FeedbackOverlayEditorY, Axis.Y);
            AnimatedElement.SwapText(m_FeedbackOverlay, Loc.Get(TextConsts.StoryReview_Editor));
            m_OverlayAnim.Replace(this, AnimatedElement.Show(m_FeedbackOverlay, 0.2f, null));
            m_ImpactLayout.Clear();
        }

        private void OnFeedbackSwapToImpact() {
            if (DebugService.AutoTesting) {
                return;
            }

            m_OverlayAnim.Stop();
            Game.Scripting.Interrupt(DisplayImpact());
        }

        private void OnFeedbackSwapToEditor() {
            m_OverlayAnim.Replace(this, Routine.Combine(
                m_FeedbackOverlay.RectTransform.AnchorPosTo(m_FeedbackOverlayEditorY, 0.5f, Axis.Y).Ease(Curve.Smooth),
                AnimatedElement.SwapText(m_FeedbackOverlay, Loc.Get(TextConsts.StoryReview_Editor), 0.5f)
            ));
        }

        private void OnFeedbackEnd() {
            m_ImpactLayout.Clear();
            m_OverlayAnim.Replace(this, AnimatedElement.Hide(m_FeedbackOverlay, 0.2f, null));
        }

        private void OnTutorialBegin() {
            m_CurrentLayer = m_OverLayer;
        }

        private void OnTutorialEnd() {
            if (m_CurrentLayer != m_BaseLayer) {
                Game.Scripting.Interrupt(SwitchToBaseLayer());
            }
        }

        private IEnumerator SwitchToBaseLayer() {
            yield return m_CurrentLayer.ClearAllAnimated();
            m_CurrentLayer = m_BaseLayer;
        }

        private IEnumerator DisplayNewStoryScrap(StringHash32 scrapId) {
            if (DebugService.AutoTesting) {
                yield break;
            }

            StoryScrapData data = Assets.Scrap(scrapId);

            // TODO: Localization
            yield return DisplayCustomMessage(m_BaseLayer, Loc.Get(TextConsts.Message_NewSnippet), "msg");
            yield return 0.1f;

            if (data != null) {
                StoryScrapDisplay scrap = GameText.AllocScrap(data, m_BaseLayer.Text, m_OverLayer.Pools);
                GameText.PopulateStoryScrap(scrap, data, Assets.Style("snippet"));
                GameText.AlignTextLine(scrap.Line, TextAlignment.Center);
                GameText.AdjustComputedLocations(m_BaseLayer.Text, 1);
                while(Streaming.IsLoading()) {
                    yield return null;
                }
                yield return GameText.AnimateLocations(m_BaseLayer.Text, 1);
            } else {
                yield return DisplayCustomMessage(m_BaseLayer, "ERROR: No scrap with id " + scrapId.ToDebugString(), "error");
            }

            Player.WriteVariable(HeaderUI.Var_NotesEnabled, true);
            yield return GameText.WaitForDefaultNext(m_BaseLayer.Choices, Assets.DefaultStyle, TextAnchor.MiddleCenter, false);
        }

        private void OnStatsUpdated(int[] adjustments) {
            if (DebugService.AutoTesting) {
                return;
            }

            Game.Scripting.Interrupt(DisplayStatsAdjusted(adjustments));
        }

        private IEnumerator DisplayStatsAdjusted(int[] adjustments) {
            UISystem.SetHeaderEnabled(true);

            // TODO: Implement for real
            using(PooledStringBuilder psb = PooledStringBuilder.Create()) {
                for(int i = 0; i < Stats.Count; i++) {
                    int adjust = adjustments[i];
                    if (adjust != 0) {
                        if (psb.Builder.Length > 0) {
                            psb.Builder.Append(" ");
                        }
                        string name = Loc.Get(Stats.Name((StatId) i));
                        if (adjust > 0) {
                            psb.Builder.Append("<b>").Append(name).Append("</b>").Append('+', adjust);
                        } else {
                            psb.Builder.Append("<b>").Append(name).Append("</b>").Append('-', -adjust);
                        }
                    }
                }

                yield return DisplayCustomMessage(m_OverLayer, psb.ToString(), "msg");
                yield return 0.8f; //time to display stat change before dismissing
                yield return m_OverLayer.ClearLines();

                Player.WriteVariable(HeaderUI.Var_StatsEnabled, true);
                yield return UISystem.SimpleTutorial("Stats");
            }
        }

        private void HandleFreeStoryStat(TextLine line) {
            line.gameObject.SetActive(false);
            line.transform.SetParent(m_BaseLayer.Pools.LinePool.PoolTransform);
        }

        private void OnChoiceOptionsUpdated() {
            // Add markers to the map
            if (m_Map.gameObject.activeSelf) {
                MapMarkerLoader.PopulateMapWithMarkers(m_Map.Texture, m_BaseLayer.AltColumn.gameObject);
            }
        }

        private void OnChoicesClearing() {
            MapMarkerLoader.ClearMarkerContainer(m_BaseLayer.AltColumn.gameObject);
        }

        #endregion // Events

        #region Columns

        private IEnumerator LoadColumnData(TagEventData evtData, StringSlice path) {
            if (evtData.Type == GameText.Events.Map) {
                m_Map.gameObject.SetActive(true);
                m_Image.gameObject.SetActive(false);

                m_Map.Texture.Path = path.ToString();
                m_Map.Texture.Preload();
                while(m_Map.Texture.IsLoading()) {
                    yield return null;
                }

                // resize texture to match root width
                Vector2 dimsRatio = new Vector2(1,
                    m_Map.transform.parent.GetComponent<RectTransform>().rect.height
                    / m_Map.transform.parent.GetComponent<RectTransform>().rect.width);

                RectTransform mapTexRect = m_Map.Texture.GetComponent<RectTransform>();
                mapTexRect.sizeDelta =
                    new Vector2(mapTexRect.rect.width * dimsRatio.x, mapTexRect.rect.width * dimsRatio.y);
            } else if (evtData.Type == GameText.Events.Image || evtData.Type == GameText.Events.Portrait) {
                m_Map.gameObject.SetActive(false);
                m_Image.gameObject.SetActive(true);

                m_Image.Texture.Path = path.ToString();
                m_Image.Texture.Preload();
                while(m_Image.Texture.IsLoading()) {
                    yield return null;
                }
            }
        }

        private TextDisplayLayer.DesiredColumnState CheckColumnLoad(TagEventData evtData, StringSlice path) {
            if (evtData.Type == GameText.Events.Map && m_Map.Texture.Path != path) {
                return path.IsEmpty ? TextDisplayLayer.DesiredColumnState.Unload : TextDisplayLayer.DesiredColumnState.Reload;
            } else if ((evtData.Type == GameText.Events.Image || evtData.Type == GameText.Events.Portrait) && m_Image.Texture.Path != path) {
                return path.IsEmpty ? TextDisplayLayer.DesiredColumnState.Unload : TextDisplayLayer.DesiredColumnState.Reload;
            } else {
                return TextDisplayLayer.DesiredColumnState.NoChange;
            }
        }

        private void UnloadColumnData() {
            m_Image.Texture.Unload();
            m_Image.Texture.Path = string.Empty;
            m_Image.gameObject.SetActive(false);

            m_Map.Texture.Unload();
            m_Map.Texture.Path = string.Empty;
            m_Map.gameObject.SetActive(false);
        }

        #endregion // Columns

        #region Clear

        public void ClearAll() {
            m_BaseLayer.ClearAll();
            m_OverLayer.ClearAll();

            m_FinishedStoryLayout.gameObject.SetActive(false);
            m_ImpactLayout.gameObject.SetActive(false);
            m_ImpactLayout.Clear();
            AnimatedElement.Hide(m_FeedbackOverlay);
            m_OverlayAnim.Stop();

            m_CurrentLayer = m_BaseLayer;
        }

        public IEnumerator ClearAllAnimated() {
            yield return m_CurrentLayer.ClearAllAnimated();
            m_CurrentLayer = m_BaseLayer;
        }

        #endregion // Clear

        #region Custom Displays

        static private IEnumerator DisplayCustomMessage(TextDisplayLayer layer, string text, StringHash32 style) {
            TextLine line = GameText.AllocLine(layer.Text, layer.Pools);
            GameText.PopulateTextLine(line, text, null, default, Assets.Style("msg"), null, layer.Text.MaxTextWidth);
            GameText.AlignTextLine(line, TextAlignment.Center);
            GameText.AdjustComputedLocations(layer.Text, 1);
            yield return GameText.AnimateLocations(layer.Text, 1);
            GameText.ClearOverflowLines(layer.Text);
        }

        public IEnumerator DisplayNewspaper() {
            UISystem.SetHeaderEnabled(false);
            yield return ClearAllAnimated();

            m_FinishedStoryLayout.gameObject.SetActive(true);
            m_FinishedStoryLayout.Root.SetAnchorPos(-660, Axis.Y);
            m_FinishedStoryLayout.Root.SetRotation(RNG.Instance.NextFloat(-1, 1), Axis.Z, Space.Self);
            yield return null;
            StoryText.LayoutNewspaper(m_FinishedStoryLayout, Assets.CurrentLevel.Story, Player.Data);
            while(Streaming.IsLoading()) {
                yield return null;
            }
            yield return m_FinishedStoryLayout.Root.AnchorPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            yield return 1;
            yield return GameText.WaitForPlayerNext(m_CurrentLayer.Choices, Loc.Get(TextConsts.TalkToEditor), Assets.Style(GameText.Characters.Action), TextAnchor.LowerRight);
            yield return m_FinishedStoryLayout.Root.AnchorPosTo(660, 0.5f, Axis.Y).Ease(Curve.BackIn);
            m_FinishedStoryLayout.gameObject.SetActive(false);
        }

        public IEnumerator DisplayImpact() {
            UISystem.SetHeaderEnabled(false);
            yield return ClearAllAnimated();

            m_ImpactLayout.gameObject.SetActive(true);
            m_ImpactLayout.Root.SetAnchorPos(660f, Axis.Y);
            m_ImpactLayout.Root.SetRotation(RNG.Instance.NextFloat(-0.2f, 0.2f), Axis.Z, Space.Self);
            yield return null;

            StoryText.CullFeedback(m_ImpactLayout.Items, m_ImpactLayout.Pins.Length);
            while(Streaming.IsLoading()) {
                yield return null;
            }

            StoryText.LayoutFeedback(m_ImpactLayout);

            m_OverlayAnim.Replace(this, Routine.Combine(
                m_FeedbackOverlay.RectTransform.AnchorPosTo(m_FeedbackOverlayImpactY, 0.5f, Axis.Y).Ease(Curve.Smooth),
                AnimatedElement.SwapText(m_FeedbackOverlay, Loc.Get(TextConsts.StoryReview_Impact), 0.5f)
            ));

            yield return m_ImpactLayout.Root.AnchorPosTo(0, 0.5f, Axis.Y).Ease(Curve.CubeOut);
            yield return 0.2f;
            yield return StoryText.AnimateFeedback(m_ImpactLayout);
            yield return GameText.WaitForDefaultNext(m_CurrentLayer.Choices, Assets.Style(GameText.Characters.Action), TextAnchor.LowerRight, false);
            yield return m_ImpactLayout.Root.AnchorPosTo(660, 0.5f, Axis.Y).Ease(Curve.BackIn);
            m_ImpactLayout.gameObject.SetActive(false);
        }

        public void EnqueueFeedbackItem(StringHash32 snippetId, StringHash32 location) {
            Game.Scripting.OverrideDisplay(m_ImpactLayout);
            m_ImpactLayout.Enqueue(snippetId, location);
        }
    
        #endregion // Custom Displays

        #region Leaf

        public IEnumerator ShowChoice(LeafChoice inChoice, LeafThreadState inThread, ILeafPlugin inPlugin) {
            return m_CurrentLayer.ShowChoice(inChoice, inThread, inPlugin);
        }

        public TagStringEventHandler PrepareLine(TagString inString, TagStringEventHandler inBaseHandler) {
            return m_CurrentLayer.PrepareLine(inString, inBaseHandler);
        }

        public IEnumerator TypeLine(TagString inSourceString, TagTextData inType) {
            return m_CurrentLayer.TypeLine(inSourceString, inType);
        }

        public IEnumerator CompleteLine() {
            return m_CurrentLayer.CompleteLine();
        }

        #endregion // Leaf
    }
}