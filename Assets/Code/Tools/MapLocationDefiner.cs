using BeauUtil;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Journalism
{
    public class MapLocationDefiner : MonoBehaviour
    {
        [SerializeField] private MapLocationDef m_mapLocationDef;
        [SerializeField] private RectTransform m_mapRect;

        [SerializeField] private Transform m_markerTransform;
        [SerializeField] private string m_locationName;

        [ContextMenu("DefineLocation")]
        public void DefineLocation() {
            Vector2 normalizedCoords = new Vector2(m_markerTransform.localPosition.x / m_mapRect.rect.width, m_markerTransform.localPosition.y / m_mapRect.rect.height);
            m_mapLocationDef.DefineLocation(m_locationName, normalizedCoords);
#if UNITY_EDITOR
            EditorUtility.SetDirty(m_mapLocationDef);
#endif

            // TODO: scale marker dimensions to match map (not just coords)
        }

        private void Awake() {
            // object should not be active outside of Editor
            this.gameObject.SetActive(false);
        }

    }
}
