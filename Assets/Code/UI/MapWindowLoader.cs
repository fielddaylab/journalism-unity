﻿using UnityEngine;
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
            MapMarkerLoader.Setup(m_markerContainerPrefab, m_streamingTex);
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
        private static MapMarkerContainer m_markerContainer;
        private static StringHash32[] m_choiceLocations;

        private static Dictionary<GameObject, MapMarkerContainer> m_containerDict;
        private static Dictionary<GameObject, MarkerStream> m_streamDict;
        private static Vector2 m_markerScale;
        private static MarkerScaling m_markerScaling;

        private static Vector2 REF_MAP_DIMS = new Vector2(745, 500); // the map dimensions markers are scaled according to

        private struct MarkerScaling
        {
            public float X;
            public float Y;
            public float TextHeight;
            public float Font;

            public MarkerScaling(float inX, float inY, float inTextHeight, float inFont) {
                X = inX;
                Y = inY;
                TextHeight = inTextHeight;
                Font = inFont;
            }
        }

        private struct MarkerStream
        {
            public int CurrStreamIndex;
            public List<LocIndexPair> CoveredLocations;

            public MarkerStream(int inStreamIndex, List<LocIndexPair> inCoveredLocations) {
                CurrStreamIndex = inStreamIndex;
                CoveredLocations = inCoveredLocations;
            }
        }

        private struct LocIndexPair
        {
            public StringHash32 LocationId;
            public int MarkerIndex;

            public LocIndexPair(StringHash32 inLocationId, int inMarkerIndex) {
                LocationId = inLocationId;
                MarkerIndex = inMarkerIndex;
            }
        }

        #region Unity Callbacks 

        public static void Setup(GameObject containerPrefab, StreamingUGUITexture mapTex) {
            // Register handler to load choice options for marker placement
            Game.Events.Register<StringHash32[]>(GameEvents.ChoiceOptionsUpdated, OnChoiceOptionsUpdated);
            Game.Events.Register(GameEvents.ChoiceCompleted, OnChoiceCompleted);

            m_markerContainerPrefab = containerPrefab;
            m_markerContainer = m_markerContainerPrefab.GetComponent<MapMarkerContainer>();

            m_containerDict = new Dictionary<GameObject, MapMarkerContainer>();
            m_streamDict = new Dictionary<GameObject, MarkerStream>();

            // normalize marker scales
            MapMarker sampleMarker = m_markerContainerPrefab.GetComponent<MapMarkerContainer>().Markers[0];
            Vector2 markerDims = new Vector2(sampleMarker.Root.rect.width, sampleMarker.Root.rect.height);

            //Rect baseMapRect = mapTex.GetComponent<RectTransform>().rect;
            //new Vector2(baseMapRect.width, baseMapRect.height);
            Vector2 mapDims = REF_MAP_DIMS;

            m_markerScale = new Vector2(markerDims.x / mapDims.x, markerDims.y / mapDims.y);

            m_markerScaling = new MarkerScaling(
                markerDims.x / mapDims.x,
                markerDims.y / mapDims.y,
                sampleMarker.TextRect.anchoredPosition.y / mapDims.y,
                sampleMarker.NumText.fontSize / mapDims.y
                );
        }

        #endregion // Unity Callbacks

        #region Member Functions

        public static void PopulateMapWithMarkers(StreamingUGUITexture mapTex, GameObject owner) {
            // Save map rect
            RectTransform mapRect = mapTex.GetComponent<RectTransform>();

            // Generate marker container
            MapMarkerContainer container = GameObject.Instantiate(m_markerContainerPrefab, owner.transform)
                .GetComponent<MapMarkerContainer>();

            // Save playermarker rect
            RectTransform playerMarkerRect = container.PlayerMarker.GetComponent<RectTransform>();

            // Track the newly created container
            m_containerDict.Add(owner, container);

            // Set all non-player markers inactive
            foreach (MapMarker marker in container.Markers) {
                marker.Root.gameObject.SetActive(false);
            }

            // Save map dims for setting marker dims
            Vector2 mapDims = new Vector2(mapRect.rect.width, mapRect.rect.height);


            // Set player location
            // set PlayerMarker location banner text
            MapLocationDef.MapLocation playerLoc = MapLocations.GetMapLocation(Player.Location());
            container.PlayerMarker.LocationIDText.SetText(playerLoc.Name);
            playerMarkerRect.sizeDelta = new Vector2(m_markerScale.x * mapDims.x, m_markerScale.y * mapDims.y);
            playerMarkerRect.localPosition = FitCoords(playerLoc.NormalizedCoords, mapRect);

            if (m_choiceLocations == null) {
                return;
            }

            // Load choice locations into markers
            int markerIndex = 0;
            List<StringHash32> coveredLocations = new List<StringHash32>();
            coveredLocations.Add(Player.Location());
            // For each choice location:
            foreach (StringHash32 locId in m_choiceLocations) {
                if (locId != StringHash32.Null) {
                    if (!coveredLocations.Contains(locId)) {
                        // position the marker at the correct location
                        MapMarker marker = container.Markers[markerIndex];
                        PlaceMarker(marker, mapDims, mapRect, MapLocations.GetMapLocation(locId).NormalizedCoords);

                        // progress to next marker
                        markerIndex++;

                        coveredLocations.Add(locId);
                    }
                    else {
                        // TODO: Handle multiple tasks at one location 
                    }
                }
            }
        }

        public static void ClearMarkerContainer(GameObject owner) {
            if (m_containerDict.ContainsKey(owner)) {
                GameObject.Destroy(m_containerDict[owner].gameObject);
                m_containerDict.Remove(owner);
                return;
            }
        }

        public static void OpenMarkerStream(GameObject requester) {
            if (!m_streamDict.ContainsKey(requester)) {
                MarkerStream newStream = new MarkerStream(0, new List<LocIndexPair>());
                m_streamDict.Add(requester, newStream);
            }
        }

        public static MapMarker StreamIn(StringHash32 locId, GameObject requester) {
            if (locId == Player.Location()) {
                // return player marker
                return m_markerContainer.PlayerMarker;
            }
            else if (locId == StringHash32.Null) {
                return null;
            }
            else {
                int streamIndex;
                MarkerStream stream;
                if (m_streamDict.ContainsKey(requester)) {
                    // Find established marker for existing location
                    stream = m_streamDict[requester];

                    foreach (LocIndexPair pair in stream.CoveredLocations) {
                        if (pair.LocationId == locId) {
                            return m_markerContainer.Markers[pair.MarkerIndex];
                        }
                    }
                }
                else {
                    // Stream has not been opened
                    return null;
                }

                // return next marker in sequence for new location
                streamIndex = m_streamDict[requester].CurrStreamIndex;

                stream = m_streamDict[requester];
                stream.CoveredLocations.Add(new LocIndexPair(locId, streamIndex));

                MapMarker marker = m_markerContainer.Markers[streamIndex];
                m_streamDict[requester] = new MarkerStream(streamIndex + 1, stream.CoveredLocations);

                return marker;
            }
        }

        public static void CloseMarkerStream(GameObject requester) {
            if (m_streamDict.ContainsKey(requester)) {
                m_streamDict.Remove(requester);
            }
        }

        #endregion // Member Functions

        #region Helper Methods

        private static void PlaceMarker(MapMarker marker, Vector2 mapDims, RectTransform mapRect, Vector2 normalizedCoords) {
            ScaleMarker(marker, mapDims);
            marker.Root.localPosition = FitCoords(normalizedCoords, mapRect);
            marker.gameObject.SetActive(true);
        }

        private static void ScaleMarker(MapMarker marker, Vector2 mapDims) {
            marker.Root.sizeDelta = new Vector2(m_markerScale.x * mapDims.x, m_markerScale.y * mapDims.y);
            // scale the text size
            marker.TextRect.anchoredPosition = new Vector2(
                marker.TextRect.anchoredPosition.x,
                m_markerScaling.TextHeight * mapDims.y
                );
            marker.NumText.fontSize = m_markerScaling.Font * mapDims.y;
        }

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