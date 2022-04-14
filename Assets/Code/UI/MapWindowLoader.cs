using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using System.Collections;
using EasyAssetStreaming;
using BeauPools;
using Leaf;

namespace Journalism.UI
{
    [RequireComponent(typeof(HeaderWindow))]
    public sealed class MapWindowLoader : MonoBehaviour
    {
        public PlayerMarker PlayerMarker;
        public RectTransform[] MapMarkerRects; // TODO: populate this array depending on max number of choices

        [SerializeField] private StreamingUGUITexture m_streamingTex;

        private StringHash32[] m_choiceLocations;
        private RectTransform m_playerMarkerRect;
        private RectTransform m_mapRect;

        private void Awake() {
            m_playerMarkerRect = PlayerMarker.GetComponent<RectTransform>();
            m_mapRect = m_streamingTex.GetComponent<RectTransform>();

            GetComponent<HeaderWindow>().LoadDataAsync = this.LoadDataAsync;
        }

        private void Start() {
            // Register handler to load choice options for marker placement
            Game.Events.Register<StringHash32[]>(GameEvents.ChoiceOptionsUpdated, OnChoiceOptionsUpdated, this);
        }

        private IEnumerator LoadDataAsync() {

            // wait for the map's streaming texture to finish loading
            while (!m_streamingTex.IsLoaded()) {
                yield return null;
            }

            // Set all non-player markers inactive
            foreach (RectTransform rect in MapMarkerRects) {
                rect.gameObject.SetActive(false);
            }

            if (m_choiceLocations == null) {
                yield break;
            }

            // Set player location
            // set PlayerMarker location banner text
            MapLocationDef.MapLocation playerLoc = MapLocations.GetMapLocation(Player.Location());
            PlayerMarker.LocationIDText.SetText(playerLoc.Name);
            m_playerMarkerRect.localPosition = FitCoords(playerLoc.NormalizedCoords, m_mapRect);

            // Load choice locations into markers
            int markerIndex = 0;
            // For each choice location:
            foreach (SerializedHash32 locId in m_choiceLocations) {
                if (locId != Player.Location()) {
                    // position the marker at the correct location
                    RectTransform positionRect = MapMarkerRects[markerIndex];
                    positionRect.localPosition = FitCoords(MapLocations.GetMapLocation(locId).NormalizedCoords, m_mapRect);
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

        #region Event Handlers

        private void OnChoiceOptionsUpdated(StringHash32[] locIds) {
            m_choiceLocations = locIds;
        }

        #endregion // Event Handlers
    }
}