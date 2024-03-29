using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using System;
using BeauPools;
using BeauUtil.Variants;

namespace Journalism {
    public sealed class TextChoice : MonoBehaviour {
        
        [Serializable] public sealed class Pool : SerializablePool<TextChoice> { }

        #region Inspector

        public TextLine Line;
        public Button Button;
        public Image Radial;
        public Image QuestionMark;
        
        public Image Marker;
        public TMP_Text MarkerNum;

        #endregion // Inspector

        [NonSerialized] public Variant TargetId;
        [NonSerialized] public uint TimeCost;
        
        [NonSerialized] public bool Selected;
        [NonSerialized] public uint ChoiceType;
        [NonSerialized] public StringHash32 LocationId;

        private void Awake() {
            Button.onClick.AddListener(() => Selected = true);
        }
    }
}