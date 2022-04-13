using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using System.Collections;

namespace Journalism.UI
{
    [RequireComponent(typeof(HeaderWindow))]
    public sealed class MapWindowLoader : MonoBehaviour
    {
        public PlayerMarker PlayerMarker;
        public RectTransform[] MapMarkerRects; // TODO: populate this array depending on max number of choices

        [SerializeField] private RectTransform m_mapRect;
        [SerializeField] private MapLocationDef m_mapLocationDef;

        private RectTransform m_playerMarkerRect;

        private void Awake() {
            m_playerMarkerRect = PlayerMarker.GetComponent<RectTransform>();

            GetComponent<HeaderWindow>().LoadDataAsync = this.LoadDataAsync;
        }

        private IEnumerator LoadDataAsync() {

            // wait for the map's streaming texture to finish loading
            // TODO: find a better way to wait for texture to be loaded (this fails if rect is square) 
            Vector2 m_currMapRect = new Vector2(m_mapRect.rect.width, m_mapRect.rect.height);

            while (m_currMapRect.x == 0 || m_currMapRect.x == m_currMapRect.y) {
                yield return null;
                m_currMapRect = new Vector2(m_mapRect.rect.width, m_mapRect.rect.height);
            }


            // Set all non-player markers inactive
            foreach (RectTransform rect in MapMarkerRects) {
                rect.gameObject.SetActive(false);
            }

            // TODO: Get Current Choice Locations
                // Add an event to TextDisplaySystem for each of when choices are loaded and unloaded?
                // Make MapWindowSingleton?
                // Some other solution?
            SerializedHash32[] choiceLocations;

            // TEMP TEST
            choiceLocations = new SerializedHash32[] { "Apartment", "Shop", "Newsroom", "Bakery" };
            Player.SetLocation("Apartment");

            if (choiceLocations == null) {
                yield break;
            }

            // Set player location
            // set PlayerMarker location banner text
            PlayerMarker.LocationIDText.SetText(Player.Location().ToString()); // TODO: make this the non-hashed string
            Vector2 normalizedCoords = m_mapLocationDef.GetNormalizedCoords(Player.Location());
            m_playerMarkerRect.localPosition = FitCoords(normalizedCoords, m_mapRect);

            // Load choice locations into markers
            int markerIndex = 0;
            // For each choice location:
            foreach (SerializedHash32 loc in choiceLocations) {
                if (loc != Player.Location()) {
                    // position the marker at the correct location
                    RectTransform positionRect = MapMarkerRects[markerIndex];
                    positionRect.localPosition = FitCoords(m_mapLocationDef.GetNormalizedCoords(loc), m_mapRect);
                    positionRect.gameObject.SetActive(true);

                    // progress to next marker
                    markerIndex++;
                }
            }
        }

        #region Helper Methods

        private Vector2 FitCoords(Vector2 normalizedCoords, RectTransform imageRect) {
            Vector2 newCoords = new Vector2(normalizedCoords.x * imageRect.rect.width,
                normalizedCoords.y * imageRect.rect.height);

            return newCoords;
        }

        #endregion // Helper Methods
    }
}