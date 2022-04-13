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
            public SerializedHash32 Id;
            public Vector2 NormalizedCoords;

            /*
            public StatId Id;

            [Header("Text")] // TODO: Localize these!!
            public string Name;
            public string[] RankNames;

            [Header("Image")]
            public Sprite Icon;

            [NonSerialized] public ushort RankInterval;
            */

            public MapLocation(SerializedHash32 id, Vector2 coords) {
                Id = id;
                NormalizedCoords = coords;
            }
        }


        [SerializeField] private MapLocation[] m_MapLocations = null;

        private Dictionary<SerializedHash32, Vector2> locationDict;

        /*
        [SerializeField] private ushort m_MaxValue = 30;
        [SerializeField] private Gradient m_StatColorGradient = null;

        [NonSerialized] private bool m_Processed;
        */



        public void DefineLocation(SerializedHash32 id, Vector2 coords) {
            // TODO: check if id is already present
            // if so, update instead of append

            // append to list of locations
            int currLength = m_MapLocations.Length;
            System.Array.Resize(ref m_MapLocations, (currLength) + 1);
            m_MapLocations[currLength] = new MapLocation(id, coords);
        }

        #region Data Retrieval

        public Vector2 GetNormalizedCoords(SerializedHash32 locationId) {
            // initialize the dict if it does not exist
            if (locationDict == null) {
                locationDict = new Dictionary<SerializedHash32, Vector2>();
                foreach (MapLocation loc in m_MapLocations) {
                    locationDict.Add(loc.Id, loc.NormalizedCoords);
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

        #endregion // Data Retrieval

        /*
        public ushort MaxValue {
            get { return m_MaxValue; }
        }
        */

        /*public Stat[] Stats {
            get {
                if (!m_Processed) {
                    Array.Sort(m_Stats, (a, b) => a.Id.CompareTo(b.Id));
                    foreach (var stat in m_Stats) {
                        stat.RankInterval = (ushort)((m_MaxValue + 1) / stat.RankNames.Length);
                    }
                    m_Processed = true;
                }
                return m_Stats;
            }
        }
        */

        /*
        public Gradient StatGradient {
            get { return m_StatColorGradient; }
        }
        */
    }

    /*
    /// <summary>
    /// Stat identifier.
    /// </summary>
    public enum StatId : byte
    {
        Research,
        Resourceful,
        Endurance,
        Tech,
        Social,
        Trust
    }
    */

    /// <summary>
    /// MapLocation access.
    /// </summary>
    static public class MapLocations
    {
        /*
        /// <summary>
        /// Total number of stats.
        /// </summary>
        public const int Count = 6;

        /// <summary>
        /// Number of time units per hour.
        /// </summary>
        public const int TimeUnitsPerHour = 12;

        /// <summary>
        /// Number of minutes per time unit.
        /// </summary>
        public const int MinutesPerTimeUnit = 60 / TimeUnitsPerHour;
        
        static private ushort s_MaxValue;
        static private StatsDef.Stat[] s_Stats;
        static private Gradient s_Gradient;

        static internal void Import(StatsDef def) {
            s_MaxValue = def.MaxValue;
            s_Stats = def.Stats;
            s_Gradient = def.StatGradient;
        }

        /// <summary>
        /// Converts hours to time units.
        /// </summary>
        [MethodImpl(256)]
        static public uint HoursToTimeUnits(float hours) {
            return (uint)Math.Round(hours * TimeUnitsPerHour);
        }

        /// <summary>
        /// Converts time units to hours.
        /// </summary>
        [MethodImpl(256)]
        static public float TimeUnitsToHours(uint timeUnits) {
            return (float)timeUnits / TimeUnitsPerHour;
        }

        /// <summary>
        /// Maximum stat value.
        /// </summary>
        static public int MaxValue {
            get { return s_MaxValue; }
        }

        /// <summary>
        /// Clamps the value of the given stat value.
        /// </summary>
        static public int Clamp(int statValue) {
            if (statValue < 1) {
                return 1;
            }
            return statValue > s_MaxValue ? s_MaxValue : statValue;
        }

        /// <summary>
        /// Returns the color for a specific rank.
        /// </summary>
        static public Color RankColor(ushort statValue) {
            Assert.NotNull(s_Gradient, "Stats not loaded");
            return s_Gradient.Evaluate((float)statValue / s_MaxValue);
        }

        /// <summary>
        /// Retrieves the stat definition for the given stat id.
        /// </summary>
        static public StatsDef.Stat Info(StatId statId) {
            Assert.NotNull(s_Stats, "Stats not loaded");
            return s_Stats[(int)statId];
        }


        /// <summary>
        /// Retrieves the name of the stat with the given stat id.
        /// </summary>
        static public string Name(StatId statId) {
            return Info(statId).Name;
        }

        /// <summary>
        /// Returns the label for the given stat with the given stat value.
        /// </summary>
        static public string RankLabel(StatId statId, ushort statValue) {
            var stat = Info(statId);
            int rankIndex = statValue / stat.RankInterval;
            if (rankIndex > stat.RankNames.Length - 1) {
                rankIndex = stat.RankNames.Length - 1;
            }
            return stat.RankNames[rankIndex];
        }
        */
    }
}