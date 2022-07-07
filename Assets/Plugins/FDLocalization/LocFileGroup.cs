using UnityEngine;

namespace FDLocalization {
    /// <summary>
    /// Group of localization files.
    /// </summary>
    [CreateAssetMenu(menuName = "Field Day/Localization/Localization File Group")]
    public sealed class LocFileGroup : ScriptableObject {

        [SerializeField] private LanguageId m_Language = default;
        [SerializeField] private LocFile[] m_Files = null;

        public LanguageId Language {
            get { return m_Language; }
        }
        public LocFile[] Files {
            get { return m_Files; }
        }
    }
}