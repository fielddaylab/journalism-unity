using UnityEngine;
using UnityEngine.UI;
using BeauUtil;
using TMPro;
using BeauRoutine;
using BeauPools;

namespace Journalism {
    public sealed class TextChoiceGroup : MonoBehaviour {
        
        #region Inspector

        [Header("Default Choice")]
        public CanvasGroup DefaultChoiceGroup = null;
        public TextChoice DefaultNextButton = null;
        public Sprite DefaultNextIcon = null;
        public Color DefaultNextIconColor = Color.black;

        #endregion // Inspector
    }
}