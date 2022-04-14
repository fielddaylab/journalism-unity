using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using BeauUtil;
using System.Collections;
using EasyAssetStreaming;
using BeauPools;
using Leaf;
using System.Collections.Generic;

namespace Journalism.UI
{
    [RequireComponent(typeof(HeaderWindow))]
    public sealed class MapWindowLoader : MonoBehaviour
    {
        [SerializeField] private StreamingUGUITexture m_streamingTex;
        [SerializeField] private GameObject m_markerContainerPrefab; // TODO: there is probably a more logical place to put this

        private void Awake() {
            GetComponent<HeaderWindow>().LoadDataAsync = this.LoadDataAsync;
            GetComponent<HeaderWindow>().UnloadData = this.UnloadData;
        }

        private void Start() {
            MapMarkerLoader.Setup(m_markerContainerPrefab);
        }

        private IEnumerator LoadDataAsync() {

            // wait for the map's streaming texture to finish loading
            while (!m_streamingTex.IsLoaded()) {
                yield return null;
            }

            MapMarkerLoader.PopulateMapWithMarkers(m_streamingTex, m_streamingTex.gameObject);
        }

        private void UnloadData() {
            MapMarkerLoader.ClearMarkerContainer(m_streamingTex.gameObject);
        }
    }

    /// <summary>
    /// Loads markers onto any provided map.
    /// </summary>
    public static class MapMarkerLoader
    {
        private static GameObject m_markerContainerPrefab;
        private static StringHash32[] m_choiceLocations;

        private static Dictionary<GameObject, MapMarkerContainer> m_ownerDict;

        #region Unity Callbacks 

        public static void Setup(GameObject containerPrefab) {
            // Register handler to load choice options for marker placement
            Game.Events.Register<StringHash32[]>(GameEvents.ChoiceOptionsUpdated, OnChoiceOptionsUpdated);
            Game.Events.Register(GameEvents.ChoiceCompleted, OnChoiceCompleted);

            m_markerContainerPrefab = containerPrefab;

            m_ownerDict = new Dictionary<GameObject, MapMarkerContainer>();
        }

        #endregion // Unity Callbacks

        #region Member Functions

        public static void PopulateMapWithMarkers(StreamingUGUITexture mapTex, GameObject owner) {
            // Save map rect
            RectTransform mapRect = mapTex.GetComponent<RectTransform>();

            // Generate marker container
            MapMarkerContainer container = GameObject.Instantiate(m_markerContainerPrefab, mapRect)
                .GetComponent<MapMarkerContainer>();

            // Save playermarker rect
            RectTransform playerMarkerRect = container.PlayerMarker.GetComponent<RectTransform>();

            // Track the newly created container
            m_ownerDict.Add(owner, container);

            // Set all non-player markers inactive
            foreach (RectTransform rect in container.MarkerRects) {
                rect.gameObject.SetActive(false);
            }

            if (m_choiceLocations == null) {
                return;
            }

            // Set player location
            // set PlayerMarker location banner text

            //TODO: Set location via leaf script
            if (Player.Location() == StringHash32.Null) {
                Player.SetLocation(StringHash32.Parse("Newsroom"));
            }
            MapLocationDef.MapLocation playerLoc = MapLocations.GetMapLocation(Player.Location());
            container.PlayerMarker.LocationIDText.SetText(playerLoc.Name);
            playerMarkerRect.localPosition = FitCoords(playerLoc.NormalizedCoords, mapRect);

            // Load choice locations into markers
            int markerIndex = 0;
            // For each choice location:
            foreach (SerializedHash32 locId in m_choiceLocations) {
                if (locId != Player.Location()) {
                    if (locId != StringHash32.Null) {
                        // position the marker at the correct location
                        RectTransform positionRect = container.MarkerRects[markerIndex];
                        positionRect.localPosition = FitCoords(MapLocations.GetMapLocation(locId).NormalizedCoords, mapRect);
                        positionRect.gameObject.SetActive(true);

                        // progress to next marker
                        markerIndex++;
                    }
                }
            }
        }

        public static void ClearMarkerContainer(GameObject owner) {
            if (m_ownerDict.ContainsKey(owner)) {
                GameObject.Destroy(m_ownerDict[owner].gameObject);
                m_ownerDict.Remove(owner);
                return;
            }
        }

        #endregion // Member Functions

        #region Helper Methods

        private static Vector2 FitCoords(Vector2 normalizedCoords, RectTransform imageRect) {
            Vector2 newCoords = new Vector2(normalizedCoords.x * imageRect.rect.width,
                normalizedCoords.y * imageRect.rect.height);

            return newCoords;
        }

        #endregion // Helper Methods

        #region Event Handlers

        private static void OnChoiceOptionsUpdated(StringHash32[] locIds) {
            m_choiceLocations = locIds;
        }

        private static void OnChoiceCompleted() {
            m_choiceLocations = null;
        }

        #endregion // Event Handlers
    }
}