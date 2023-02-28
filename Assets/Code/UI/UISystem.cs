using UnityEngine;
using BeauUtil;
using Journalism.UI;
using BeauUtil.Variants;
using Leaf.Runtime;
using BeauRoutine;
using BeauRoutine.Extensions;
using System;
using UnityEngine.Scripting;
using System.Collections;
using BeauUtil.Debugger;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Journalism.UI {
    public class UISystem : MonoBehaviour {

        #region Consts

        static private readonly TableKeyPair Var_HeaderEnabled = TableKeyPair.Parse("ui:header-enabled");
        static private readonly TableKeyPair Var_ShowStory = TableKeyPair.Parse("ui:show-story");

        public const InputLayerFlags DefaultInputMask = InputLayerFlags.AllStory | InputLayerFlags.Toolbar;

        #endregion // Consts

        #region Inspector

        [SerializeField] private EventSystem m_UnityEvents = null;
        [SerializeField] private HeaderUI m_Header = null;
        [SerializeField] private HeaderWindow m_HeaderWindow = null;
        [SerializeField] private CanvasGroup m_HeaderUnderFader = null;
        [SerializeField] private CanvasGroup m_SolidBGFader = null;
        [SerializeField] private GameOverWindow m_GameOver = null;
        [SerializeField] private TitleWindow m_Title = null;
        [SerializeField] private CreditsWindow m_Credits = null;
        [SerializeField] private TutorialArrow m_TutorialArrow = null;
        [SerializeField] private AnimatedElement m_CheckpointNotification = null;
        [SerializeField] private float m_CheckpointNotificationOffscreenPos = -300;
        [SerializeField] private float m_CheckpointNotificationOnscreenPos = -8;
        [SerializeField] private TweenSettings m_CheckpointNotificationAnim = new TweenSettings(0.3f);

        #endregion // Inspector

        private Routine m_FaderRoutine;
        private Routine m_SolidBGRoutine;
        [NonSerialized] private PointerEventData m_PointerEvtData;
        [NonSerialized] private bool m_SolidBGState;
        [NonSerialized] private InputLayer[] m_InputLayers = null;
        private readonly Dictionary<StringHash32, InputElement> m_InputElements = new Dictionary<StringHash32, InputElement>();
        private readonly RingBuffer<InputLayerFlags> m_InputStack = new RingBuffer<InputLayerFlags>();
        private InputLayerFlags m_InputMask = DefaultInputMask;

        public HeaderUI Header { get { return m_Header; } }
        public GameOverWindow GameOver { get { return m_GameOver; } }

        #region Unity Events

        private void Awake() {
            Game.Events.Register<TableKeyPair>(GameEvents.VariableUpdated, OnVariableUpdated, this)
                .Register(GameEvents.LevelLoading, OnLevelLoading, this)
                .Register(GameEvents.LevelStarted, OnLevelStarted, this)
                .Register(GameEvents.GameOver, OnGameOver, this)
                .Register(GameEvents.EditorNotesOpen, OnNeedSolidBG, this)
                .Register(GameEvents.EditorNotesClose, OnNoLongerNeedSolidBG, this)
                .Register(GameEvents.GameOverClose, OnNoLongerNeedSolidBG)
                .Register(GameEvents.RequireStoryPublish, OnRequirePublish, this)
                .Register(GameEvents.LevelCheckpoint, OnCheckpointSaved, this)
                .Register(GameEvents.LoadTitleScreen, OnTitleScreenLoad, this)
                .Register(GameEvents.RollCredits, OnRollCredits, this);

            m_HeaderUnderFader.gameObject.SetActive(false);
            m_HeaderUnderFader.alpha = 0;

            AnimatedElement.Hide(m_CheckpointNotification);
            m_CheckpointNotification.RectTransform.SetAnchorPos(m_CheckpointNotificationOffscreenPos, Axis.X);

            m_HeaderWindow.OnShowEvent.AddListener(OnHeaderShow);
            m_HeaderWindow.OnHideEvent.AddListener(OnHeaderHide);

            m_InputLayers = FindObjectsOfType<InputLayer>();
            UpdateInputLayers();

            m_PointerEvtData = new PointerEventData(m_UnityEvents);
        }

        private void OnDestroy() {
            Game.Events?.DeregisterAll(this);
        }

        #endregion // Unity Events

        #region Handlers

        private void OnVariableUpdated(TableKeyPair varId) {
            if (varId == Var_HeaderEnabled) {
                RefreshHeaderEnabled();
            }
        }

        private void OnGameOver() {
            m_HeaderWindow.Hide();
            OnNeedSolidBG();
            PushInputMask(InputLayerFlags.GameOver);
        }

        private void OnLevelLoading() {
            m_HeaderWindow.Hide();
            m_GameOver.Hide();
            OnNoLongerNeedSolidBG();
            AnimatedElement.Hide(m_CheckpointNotification);
            m_CheckpointNotification.Animation.Stop();
        }

        private void OnLevelStarted() {
            RefreshHeaderEnabled();
            m_GameOver.InstantHide();
            m_SolidBGFader.Hide();
            m_SolidBGState = false;
            m_SolidBGRoutine.Stop();
            AnimatedElement.Hide(m_CheckpointNotification);
            m_CheckpointNotification.Animation.Stop();
            m_TutorialArrow.Hide(true);
            m_Title.Hide();

            m_InputStack.Clear();
            m_InputMask = DefaultInputMask;
            UpdateInputLayers();
        }

        private void RefreshHeaderEnabled() {
            bool showHeader = Player.ReadVariable(Var_HeaderEnabled).AsBool();
            if (showHeader) {
                m_HeaderWindow.Show();
            } else {
                m_HeaderWindow.Hide();
            }
        }

        private void OnHeaderShow(BasePanel.TransitionType type) {
            if (type == BasePanel.TransitionType.Instant) {
                m_HeaderUnderFader.gameObject.SetActive(true);
                m_HeaderUnderFader.alpha = 1;
            } else if (m_HeaderUnderFader.alpha < 1) {
                m_HeaderUnderFader.gameObject.SetActive(true);
                m_FaderRoutine.Replace(this, m_HeaderUnderFader.FadeTo(1, 0.2f));
            }
        }

        private void OnCheckpointSaved() {
            Game.Audio.PlayOneShot("Checkpoint");
            m_CheckpointNotification.Animation.Replace(this, CheckpointAnim());
        }

        private IEnumerator CheckpointAnim() {
            m_CheckpointNotification.RectTransform.SetAnchorPos(m_CheckpointNotificationOffscreenPos, Axis.X);
            AnimatedElement.Show(m_CheckpointNotification);
            yield return m_CheckpointNotification.RectTransform.AnchorPosTo(m_CheckpointNotificationOnscreenPos, m_CheckpointNotificationAnim, Axis.X);
            yield return 2;
            yield return m_CheckpointNotification.RectTransform.AnchorPosTo(m_CheckpointNotificationOffscreenPos, m_CheckpointNotificationAnim, Axis.X);
            AnimatedElement.Hide(m_CheckpointNotification);
        }

        private void OnHeaderHide(BasePanel.TransitionType type) {
            if (type == BasePanel.TransitionType.Instant) {
                m_HeaderUnderFader.gameObject.SetActive(false);
                m_HeaderUnderFader.alpha = 0;
            } else if (m_HeaderUnderFader.alpha > 0) {
                m_FaderRoutine.Replace(this, m_HeaderUnderFader.FadeTo(0, 0.2f).OnComplete(() => m_HeaderUnderFader.gameObject.SetActive(false)));
            }
        }

        private void OnNeedSolidBG() {
            if (m_SolidBGState) {
                return;
            }

            m_SolidBGState = true;
            m_SolidBGRoutine.Replace(this, m_SolidBGFader.Show(0.2f));
        }

        private void OnNoLongerNeedSolidBG() {
            if (!m_SolidBGState) {
                return;
            }

            m_SolidBGState = false;
            m_SolidBGRoutine.Replace(this, m_SolidBGFader.Hide(0.2f));
        }

        private void OnRequirePublish() {
            if (DebugService.AutoTesting) {
                List<StoryScrapData> scraps = new List<StoryScrapData>();
                foreach(var scrapId in Player.StoryScraps) {
                    scraps.Add(Assets.Scrap(scrapId));
                }
                RNG.Instance.Shuffle(scraps);
                for(int i = 0; i < Assets.CurrentLevel.Story.Slots.Length; i++) {
                    var slot = Assets.CurrentLevel.Story.Slots[i];
                    StoryScrapType mask = slot.Type == StorySlotType.Any ? StoryScrapType.AnyMask : StoryScrapType.ImageMask;
                    for(int j = 0; j < scraps.Count; j++) {
                        var data = scraps[j];
                        if ((data.Type & mask) != 0) {
                            Player.Data.AllocatedScraps[i] = data.Id;
                            scraps.FastRemoveAt(j);
                            break;
                        }
                    }
                }
                Routine.StartDelay(() => Game.Events.Queue(GameEvents.StoryPublished), 0.1f);
                return;
            }
            m_Header.FindButton("Notes").Button.isOn = true;
        }

        private void OnTitleScreenLoad() {
            m_Title.Show();
        }

        private void OnRollCredits() {
            m_Credits.Show();
        }

        #endregion // Handlers

        #region Buttons

        public void RegisterInputElement(InputElement element) {
            m_InputElements.Add(element.Id.IsEmpty ? element.gameObject.name : element.Id, element);
        }

        public void DeregisterInputElement(InputElement element) {
            m_InputElements.Remove(element.Id.IsEmpty ? element.gameObject.name : element.Id);
        }

        public bool ForceClick(GameObject inRoot) {
            return ExecuteEvents.Execute(inRoot, m_PointerEvtData, ExecuteEvents.pointerClickHandler);
        }

        public bool ForceClick(StringHash32 id) {
            if (!m_InputElements.TryGetValue(id, out InputElement element)) {
                Log.Msg("[UISystem] No input element with id '{0}' is active", id);
                return false;
            }

            return ForceClick(element.gameObject);
        }

        #endregion // Buttons

        #region Tutorial

        static public IEnumerator SimpleTutorial(StringSlice windowId) {
            if (Player.WriteVariable("ui:tutorial.windowId." + windowId.ToString(), true)) {
                return TutorialRoutine(windowId);
            }

            return null;
        }

        static private IEnumerator TutorialRoutine(StringHash32 id) {
            UISystem.SetHeaderEnabled(true);
            yield return null;
            UISystem.OpenWindow(id);
            yield return null;
            while(Game.UI.Header.AnyOpen()) {
                yield return null;
            }
            yield return 0.2f;
        }

        public void PointTo(GameObject inRoot, Vector2 offset) {
            m_TutorialArrow.Focus(inRoot.transform, offset);
        }

        public void PointTo(StringHash32 id, Vector2 offset) {
            if (!m_InputElements.TryGetValue(id, out InputElement element)) {
                Log.Msg("[UISystem] No input element with id '{0}' is active", id);
                return;
            }

            PointTo(element.gameObject, offset);
        }

        public void ClearPointer() {
            m_TutorialArrow.Hide();
        }

        #endregion // Tutorial

        #region Input

        private void UpdateInputLayers() {
            foreach(var input in m_InputLayers) {
                bool active = (input.Type & m_InputMask) != 0;
                input.Raycaster.enabled = active;
            }
        }

        public void PushInputMask(InputLayerFlags mask) {
            m_InputStack.PushBack(m_InputMask);
            m_InputMask = mask;
            UpdateInputLayers();
        }

        public void PopInputMask() {
            if (m_InputStack.Count > 0) {
                m_InputMask = m_InputStack.PopBack();
            } else {
                Log.Error("[UISystem] Unbalanced input mask push/pop");
                m_InputMask = DefaultInputMask;
            }
            UpdateInputLayers();
        }

        #endregion // Input

        #region Leaf

        [LeafMember("SetHeaderEnabled"), Preserve]
        static public void SetHeaderEnabled(bool enabled) {
            Player.WriteVariable(Var_HeaderEnabled, enabled);
        }

        [LeafMember("HeaderEnabled"), Preserve]
        static public bool GetHeaderEnabled() {
            return Player.ReadVariable(Var_HeaderEnabled).AsBool();
        }

        [LeafMember("OpenWindow"), Preserve]
        static public void OpenWindow(StringHash32 id) {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.UI.m_Header.FindButton(id).Button.isOn = true;
        }

        [LeafMember("CloseWindow"), Preserve]
        static private IEnumerator CloseWindow(StringHash32 id = default) {
            if (DebugService.AutoTesting) {
                yield break;
            }
            if (id.IsEmpty) {
                Game.UI.Header.ToggleGroup.SetAllTogglesOff();
            } else {
                Game.UI.m_Header.FindButton(id).Button.isOn = false;
            }
            if (Game.UI.Header.AnyOpen()) {
                while(Game.UI.Header.AnyOpen()) {
                    yield return null;
                }
                yield return 0.2f;
            }
        }

        [LeafMember("ClickOn"), Preserve]
        static private void ClickElement(StringHash32 id) {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.UI.ForceClick(id);
        }

        [LeafMember("PointTo"), Preserve]
        static private void PointTo(StringHash32 id, float offsetX = 0, float offsetY = 16) {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.UI.PointTo(id, new Vector2(offsetX, offsetY));
        }

        [LeafMember("ClearPointer"), Preserve]
        static private void LeafClearPointer() {
            if (DebugService.AutoTesting) {
                return;
            }
            Game.UI.ClearPointer();
        }

        [LeafMember("SetStoryEnabled"), Preserve]
        static public void SetStoryEnabled(bool enabled) {
            Player.WriteVariable(Var_ShowStory, enabled);
        }

        [LeafMember("ActivateStory"), Preserve]
        static public void ActivateStory() {
            Player.WriteVariable(HeaderUI.Var_NotesEnabled, true);
            Player.WriteVariable(Var_ShowStory, true);
        }

        [LeafMember("StoryEnabled"), Preserve]
        static public bool GetStoryEnabled() {
            return Player.ReadVariable(Var_ShowStory).AsBool();
        }

        [LeafMember("RunPublish"), Preserve]
        static public IEnumerator RunPublish() {
            ActivateStory();
            Game.Events.Dispatch(GameEvents.RequireStoryPublish);
            Game.UI.PushInputMask(InputLayerFlags.Toolbar);
            yield return Game.Events.Wait(GameEvents.StoryPublished);
            Game.UI.PopInputMask();
            yield return 0.2f;
            yield return Game.Scripting.DisplayNewspaper();
        }

        [LeafMember("RollCredits"), Preserve]
        static public void RollCredits() {
            Game.Events.Dispatch(GameEvents.RollCredits);
        }

        #endregion // Leaf
    }
}