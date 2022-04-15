using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BeauUtil;
using BeauUtil.Debugger;
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
            public Vector2 NormalizedCoords;

            public MapLocation(StringHash32 id, string name, Vector2 coords) {
                Id = id;
                Name = name;
                NormalizedCoords = coords;
            }
        }

        [SerializeField] private MapLocation[] m_MapLocations = null;

        public void DefineLocation(string name, Vector2 coords) {
            // TODO: check if id is already present
            // if so, update instead of append

            // append to list of locations
            int currLength = m_MapLocations.Length;
            System.Array.Resize(ref m_MapLocations, (currLength) + 1);
            m_MapLocations[currLength] = new MapLocation(StringHash32.Parse(name), name, coords);
        }

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
        }

        public static MapLocationDef.MapLocation GetMapLocation(StringHash32 locationId) {
            // initialize the dict if it does not exist
            if (locationDict == null) {
                locationDict = new Dictionary<StringHash32, MapLocationDef.MapLocation>();
                foreach (MapLocationDef.MapLocation loc in s_MapLocations) {
                    locationDict.Add(loc.Id, loc);
                }
            }
            if (locationDict.ContainsKey(locationId)) {
                return locationDict[locationId];
            }
            else {
                throw new KeyNotFoundException(string.Format("No Map Location " +
                    "with id `{0}' is in the database", locationId
                ));
            }
        }
    }
}