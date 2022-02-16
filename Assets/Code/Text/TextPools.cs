using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;

namespace Journalism {
    public sealed class TextPools : MonoBehaviour {
        public TextLine.Pool LinePool;
        public TextChoice.Pool ChoicePool;
        
        public StoryScrapDisplay.Pool ImageStoryPool;
        public StoryScrapDisplay.Pool TextStoryPool;

        public TextLine.Pool StatChangePool;
    }
}