using UnityEngine;
using BeauUtil;
using TMPro;
using EasyAssetStreaming;
using UnityEngine.UI;
using System;
using BeauRoutine;

namespace Journalism.UI {
    public sealed class StoryBuilderSlot : MonoBehaviour {
        
        public RectTransform Size;
        public RectTransform LocalAnim;
        public GameObject Background;

        [Header("Empty")]
        public GameObject[] EmptyGroup;
        public Image EmptyIcon;
        public TMP_Text TypeText;

        [Header("Scrap")]
        public GameObject SnippetGroup;
        public TMP_Text Text;
        public StreamingUGUITexture Image;

        [NonSerialized] public StoryScrapData Data;
        [NonSerialized] public Routine Animation;
    }
}