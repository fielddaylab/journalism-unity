using UnityEngine;
using BeauUtil;
using System;
using BeauRoutine.Extensions;

namespace Journalism {
    [CreateAssetMenu(menuName = "Journalism Audio/Audio Event")]
    public sealed class AudioEvent : ScriptableObject {
        #region Inspector

        [Required] public AudioClip[] Samples = null;
        public FloatRange Volume = new FloatRange(1);

        #endregion // Inspector

        [NonSerialized] public RandomDeck<AudioClip> SampleRandomizer;
    }
}