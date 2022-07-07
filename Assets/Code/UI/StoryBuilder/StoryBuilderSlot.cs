using UnityEngine;
using BeauUtil;
using TMPro;
using EasyAssetStreaming;
using UnityEngine.UI;
using System;
using BeauRoutine;
using BeauUtil.UI;
using FDLocalization;

namespace Journalism.UI {
    public sealed class StoryBuilderSlot : MonoBehaviour {
        
        public RectTransform Size;
        public Graphic Flash;
        public CanvasGroup Group;

        [Header("Empty")]
        public GameObject EmptyGroup;
        public ColorGroup EmptyColor;
        public Image EmptyIcon;
        public LocText EmptyLabel;
        public PointerListener Click;
        public GameObject AvailableHighlight;
        public GameObject HoverHighlight;

        [Header("Scrap")]
        public StoryScrapDisplay Snippet;
        public Button RemoveButton;

        [NonSerialized] public StoryScrapType Filter;
        [NonSerialized] public int Index;
        [NonSerialized] public StoryScrapData Data;
        [NonSerialized] public Routine Animation;

        private void OnDisable() {
            Data = null;
            Animation.Stop();
        }
    }
}