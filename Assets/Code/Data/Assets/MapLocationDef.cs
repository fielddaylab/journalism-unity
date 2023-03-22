using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
using FDLocalization;
using UnityEngine;

namespace Journalism
{
    [CreateAssetMenu(menuName = "Journalism Content/Map Location Definitions")]
    public class MapLocationDef : ScriptableObject
    {
        [Serializable]
        public class MapLocation
        {
            [HideInInspector] public StringHash32 Id;
            public string Name;
            public LocId NameId;
            public Vector2 NormalizedCoords;

            public MapLocation(StringHash32 id, string name, Vector2 coords) {
                Id = id;
                Name = name;
                NormalizedCoords = coords;
            }
        }

        [SerializeField] private MapLocation[] m_MapLocations = null;

        public void DefineLocation(string name, Vector2 coords) {
            // check if id is already present
            // if so, update instead of append
            for (int i = 0; i < m_MapLocations.Length; i++) {
                if (m_MapLocations[i].Name == name) {
                    m_MapLocations[i] = new MapLocation(name, name, coords);
                    return;
                }
            }

            // else append to list of locations
            int currLength = m_MapLocations.Length;
            System.Array.Resize(ref m_MapLocations, (currLength) + 1);
            m_MapLocations[currLength] = new MapLocation(name, name, coords);
        }

        #if UNITY_EDITOR

        private void OnValidate() {
            if (Application.isPlaying) {
                return;
            }

            foreach(var loc in m_MapLocations) {
                loc.Id = loc.Name;
            }
        }

        #endif // UNITY_EDITOR

        #region Data Retrieval

        public MapLocation[] MapLocations {
            get {
                return m_MapLocations;
            }
        }

        #endregion // Data Retrieval
    }

    /// <summary>
    /// MapLocation access.
    /// </summary>
    static public class MapLocations
    {
        static private MapLocationDef.MapLocation[] s_MapLocations;
        private static Dictionary<StringHash32, MapLocationDef.MapLocation> locationDict;

        static internal void Import(MapLocationDef def) {
            s_MapLocations = def.MapLocations;

            locationDict = new Dictionary<StringHash32, MapLocationDef.MapLocation>(s_MapLocations.Length);
            foreach (MapLocationDef.MapLocation loc in s_MapLocations) {
                locationDict.Add(loc.Id, loc);
            }
        }

        public static MapLocationDef.MapLocation GetMapLocation(StringHash32 locationId) {
            MapLocationDef.MapLocation location;
            if (!locationDict.TryGetValue(locationId, out location)) {
                Assert.Fail("No map location with id '{0}' defined", locationId);
            }
            return location;
        }

        public static Vector2 NormalizedCoords(StringHash32 locationId) {
            MapLocationDef.MapLocation location;
            if (!locationDict.TryGetValue(locationId, out location)) {
                Assert.Fail("No map location with id '{0}' defined", locationId);
            }
            return location.NormalizedCoords;
        }
    }
}