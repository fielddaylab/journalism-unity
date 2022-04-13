using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;
using UnityEngine.EventSystems;
using System.Collections;
using BeauUtil.UI;

namespace Journalism.UI {
    [RequireComponent(typeof(HeaderWindow))]
    public sealed class StoryWindowCtrl : MonoBehaviour {

        [SerializeField] private TextPools m_Pools = null;

        [Header("Scrap List")]
        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private LayoutGroup m_ListLayout = null;
        [SerializeField] private PointerListener m_BGClick = null;
        [SerializeField] private ScrollRect m_ListScroll = null;
        [SerializeField] private RectTransform m_SelectedParent = null;
        [SerializeField] private RectTransform m_UnselectedParent = null;
        [SerializeField] private ContentSizeFitter m_ListFitter = null;

        [Header("Slot Grid")]
        [SerializeField] private StorySlotLayout m_SlotLayout = null;
        [SerializeField] private Color m_DefaultSlotColor = Color.white;
        [SerializeField] private Color m_AvailableSlotColor = Color.green;

        [NonSerialized] private List<StoryScrapDisplay> m_Scraps = new List<StoryScrapDisplay>();

        [NonSerialized] private int m_LastKnownLevelIdx = -1;
        [NonSerialized] private StoryScrapDisplay m_SelectedScrap = null;

        private void Awake() {

            GetComponent<HeaderWindow>().LoadDataAsync = LoadAsync;

            GetComponent<HeaderWindow>().UnloadData = () => {
                m_Pools.ScrapPool.Reset();
                m_Scraps.Clear();
                m_ToggleGroup.SetAllTogglesOff(false);
                m_SelectedScrap = null;
            };
        
            foreach(var obj in m_SlotLayout.Slots) {
                StoryBuilderSlot cachedSlot = obj;
                obj.RemoveButton.onClick.AddListener(() => OnSlotDeleteClick(cachedSlot));
                obj.Click.onPointerEnter.AddListener((p) => OnSlotHoverEnter(cachedSlot));
                obj.Click.onPointerExit.AddListener((p) => OnSlotHoverExit(cachedSlot));
                obj.Click.onClick.AddListener((p) => OnSlotClick(cachedSlot));
                obj.EmptyColor.Color = m_DefaultSlotColor;
            }

            m_BGClick.onClick.AddListener((p) => {
                if (p.button == 0) {
                    SetSelectedScrap(null);
                }
            });
        }

        private IEnumerator LoadAsync() {
            m_ListLayout.enabled = true;
            m_ListFitter.enabled = true;
            m_ListScroll.enabled = true;

            yield return null;

            var allocated = Player.AllocatedScraps;

            StoryScrapDisplay.SelectDelegate onSelectChanged = OnScrapSelected;

            foreach(var scrapId in Player.StoryScraps) {
                var scrapData = Assets.Scrap(scrapId);
                if (scrapData != null) {
                    var display = GameText.AllocScrap(scrapData, m_Pools);
                    GameText.PopulateStoryScrap(display, scrapData, Assets.DefaultStyle);
                    display.Object.Toggle.group = m_ToggleGroup;
                    display.Object.OnSelectChanged = onSelectChanged;
                    display.Object.Toggle.interactable = !allocated.Contains(scrapId);
                    yield return null;
                    m_Scraps.Add(display.Object);
                }
            }

            yield return new AsyncSleep(TimeSpan.FromSeconds(0.002f));
            m_ListLayout.ForceRebuild();
            m_ListLayout.enabled = false;
            m_ListFitter.enabled = false;

            if (Ref.Replace(ref m_LastKnownLevelIdx, Player.Data.LevelIndex)) {
                StoryText.LayoutSlots(m_SlotLayout, Assets.CurrentLevel.Story);
            }

            for(int i = 0; i < allocated.Length; i++) {
                StringHash32 allocatedId = allocated[i];
                if (allocatedId.IsEmpty) {
                    StoryText.EmptySlot(m_SlotLayout.ActiveSlots[i]);
                } else {
                    StoryText.FillSlot(m_SlotLayout.ActiveSlots[i], Assets.Scrap(allocatedId));
                }
                yield return null;
            }

            SetSelectedScrap(null);
        }

        #region Handlers

        private void OnScrapSelected(StoryScrapDisplay display, bool state) {
            if (!state && m_SelectedScrap == display) {
                SetSelectedScrap(null);
            } else {
                SetSelectedScrap(display);
            }
        }

        private void OnSlotHoverEnter(StoryBuilderSlot slot) {
            if (!CanAccept(slot, m_SelectedScrap)) {
                return;
            }

            slot.HoverHighlight.SetActive(true);
        }

        private void OnSlotHoverExit(StoryBuilderSlot slot) {
            slot.HoverHighlight.SetActive(false);
        }

        private void OnSlotClick(StoryBuilderSlot slot) {
            if (!CanAccept(slot, m_SelectedScrap)) {
                return;
            }

            if (SetSlot(slot, m_SelectedScrap.Data.Id)) {
                slot.Animation.Replace(this, FlashAnimation(slot.Flash));
                SetSelectedScrap(null);
            }
        }

        private void OnSlotDeleteClick(StoryBuilderSlot slot) {
            if (ClearSlot(slot)) {
                slot.Animation.Replace(this, FlashAnimation(slot.Flash));
                SetSelectedScrap(null);
            }
        }

        #endregion // Handlers

        #region Slots

        private bool SetSlot(StoryBuilderSlot slot, StringHash32 id) {
            if (id.IsEmpty) {
                return ClearSlot(slot);
            }
            
            if (slot.Data != null && slot.Data.Id == id) {
                return false;
            }

            ClearSlot(slot);
            Player.Data.AllocatedScraps[slot.Index] = id;
            StoryText.FillSlot(slot, Assets.Scrap(id));
            var scrap = FindScrapWithId(id);
            if (scrap) {
                scrap.Toggle.interactable = false;
            }

            return true;
        }

        private bool ClearSlot(StoryBuilderSlot slot) {
            var current = slot.Data;
            if (current != null) {
                StoryText.EmptySlot(slot);
                Player.Data.AllocatedScraps[slot.Index] = default;
                var scrap = FindScrapWithId(current.Id);
                if (scrap) {
                    scrap.Toggle.interactable = true;
                }
                return true;
            }

            return false;
        }

        static private bool CanAccept(StoryBuilderSlot slot, StoryScrapDisplay display) {
            return display && slot.Data == null && (slot.Filter & display.Data.Type) != 0;
        }

        #endregion // Slots

        #region Scraps

        private void SetSelectedScrap(StoryScrapDisplay display) {
            if (m_SelectedScrap == display) {
                return;
            }

            if (m_SelectedScrap) {
                m_SelectedScrap.Line.Root.SetParent(m_UnselectedParent);
                m_SelectedScrap.Animation.Replace(this, StopHovering(m_SelectedScrap));
                m_SelectedScrap.Toggle.SetIsOnWithoutNotify(false);
            }

            m_SelectedScrap = display;
            m_ListScroll.enabled = !display;
            m_ListScroll.verticalScrollbar.enabled = !display;

            if (display) {
                foreach(var slot in m_SlotLayout.ActiveSlots) {
                    bool canAccept = CanAccept(slot, display);
                    slot.AvailableHighlight.SetActive(canAccept);
                    slot.Group.alpha = canAccept ? 1 : 0.5f;
                    slot.EmptyColor.Color = canAccept ? m_AvailableSlotColor : m_DefaultSlotColor;
                }
            
                display.Line.Root.SetParent(m_SelectedParent);
                display.Animation.Replace(this, HoverScrap(display));
                m_SelectedScrap.Toggle.SetIsOnWithoutNotify(true);
            } else {
                foreach(var slot in m_SlotLayout.ActiveSlots) {
                    slot.Group.alpha = 1;
                    slot.AvailableHighlight.SetActive(false);
                    slot.HoverHighlight.SetActive(false);
                    slot.EmptyColor.Color = m_DefaultSlotColor;
                }
            }
        }

        private StoryScrapDisplay FindScrapWithId(StringHash32 id) {
            foreach(var scrap in m_Scraps) {
                if (scrap.Data.Id == id) {
                    return scrap;
                }
            }

            return null;
        }

        #endregion // Scraps

        #region Animations

        static private IEnumerator FlashAnimation(Graphic graphic) {
            graphic.gameObject.SetActive(true);
            graphic.SetAlpha(1);
            yield return null;
            yield return graphic.FadeTo(0, 0.2f).Ease(Curve.CubeIn);
            graphic.gameObject.SetActive(false);
        }

        static private IEnumerator HoverScrap(StoryScrapDisplay display) {
            while(true) {
                yield return display.Line.Inner.AnchorPosTo(8, 0.6f, Axis.Y).Ease(Curve.Smooth);
                yield return display.Line.Inner.AnchorPosTo(0, 0.6f, Axis.Y).Ease(Curve.Smooth);
            }
        }

        static private IEnumerator StopHovering(StoryScrapDisplay display) {
            yield return display.Line.Inner.AnchorPosTo(0, 0.1f, Axis.Y);
        }

        #endregion // Animations
    }
}