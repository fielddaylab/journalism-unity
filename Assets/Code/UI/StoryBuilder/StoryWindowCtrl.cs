using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using BeauUtil;
using BeauRoutine;

namespace Journalism.UI {
    [RequireComponent(typeof(HeaderWindow))]
    public sealed class StoryWindowCtrl : MonoBehaviour {

        [SerializeField] private TextPools m_Pools = null;
        [SerializeField] private ToggleGroup m_ToggleGroup = null;
        [SerializeField] private LayoutGroup m_ListLayout = null;
        [SerializeField] private ContentSizeFitter m_ListFitter = null;

        [NonSerialized] private List<StoryScrapDisplay> m_Scraps = new List<StoryScrapDisplay>();

        private void Awake() {
            GetComponent<HeaderWindow>().LoadData = () => {
                m_ListLayout.enabled = true;
                m_ListFitter.enabled = true;

                foreach(var scrapId in Player.StoryScraps) {
                    var scrapData = Assets.Scrap(scrapId);
                    if (scrapData != null) {
                        var display = GameText.AllocScrap(scrapData, m_Pools);
                        GameText.PopulateStoryScrap(display, scrapData, Assets.DefaultStyle);
                        display.Object.Toggle.group = m_ToggleGroup;
                    }
                }

                Async.InvokeAsync(() => {
                    m_ListLayout.ForceRebuild();
                    m_ListLayout.enabled = false;
                    m_ListFitter.enabled = false;
                });
            };

            GetComponent<HeaderWindow>().UnloadData = () => {
                m_Pools.TextStoryPool.Reset();
                m_Pools.ImageStoryPool.Reset();
                m_Scraps.Clear();
                m_ToggleGroup.SetAllTogglesOff(false);
            };
        }
    }
}