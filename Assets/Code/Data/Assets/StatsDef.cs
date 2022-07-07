using System;
using System.Runtime.CompilerServices;
using BeauUtil.Debugger;
using FDLocalization;
using UnityEngine;

namespace Journalism {
    [CreateAssetMenu(menuName = "Journalism Content/Stat Definitions")]
    public class StatsDef : ScriptableObject {
        [Serializable]
        public class Stat {
            public StatId Id;
            
            [Header("Text")]
            public LocId NameId;
            public LocId[] RankNameIds;

            [Header("Image")]
            public Sprite Icon;

            [NonSerialized] public ushort RankInterval;
        }

        [SerializeField] private Stat[] m_Stats = null;
        [SerializeField] private ushort m_MaxValue = 30;
        [SerializeField] private Color[] m_StatColors = null;
        
        [NonSerialized] private bool m_Processed;

        public ushort MaxValue {
            get { return m_MaxValue; }
        }

        public Stat[] Stats {
            get {
                if (!m_Processed) {
                    Array.Sort(m_Stats, (a, b) => a.Id.CompareTo(b.Id));
                    foreach(var stat in m_Stats) {
                        stat.RankInterval = (ushort) (m_MaxValue / (stat.RankNameIds.Length - 1));
                    }
                    m_Processed = true;
                }
                return m_Stats;
            }
        }


        public Color[] StatColors {
            get { return m_StatColors; }
        }
    }

    /// <summary>
    /// Stat identifier.
    /// </summary>
    public enum StatId : byte {
        Research,
        Resourceful,
        Endurance,
        Tech,
        Social,
        Trust
    }

    /// <summary>
    /// Stat access.
    /// </summary>
    static public class Stats {
        /// <summary>
        /// Total number of stats.
        /// </summary>
        public const int Count = 6;

        /// <summary>
        /// Number of time units per hour.
        /// </summary>
        public const int TimeUnitsPerHour = 4;

        /// <summary>
        /// Number of minutes per time unit.
        /// </summary>
        public const int MinutesPerTimeUnit = 60 / TimeUnitsPerHour;

        /// <summary>
        /// Minimum stat value.
        /// </summary>
        public const int MinValue = 0;

        static private ushort s_MaxValue;
        static private StatsDef.Stat[] s_Stats;
        static private Color[] s_Gradient;

        static internal void Import(StatsDef def) {
            s_MaxValue = def.MaxValue;
            s_Stats = def.Stats;
            s_Gradient = def.StatColors;
        }

        /// <summary>
        /// Converts hours to time units.
        /// </summary>
        [MethodImpl(256)]
        static public uint HoursToTimeUnits(float hours) {
            return (uint) Math.Round(hours * TimeUnitsPerHour);
        }

        /// <summary>
        /// Converts time units to hours.
        /// </summary>
        [MethodImpl(256)]
        static public float TimeUnitsToHours(uint timeUnits) {
            return (float) timeUnits / TimeUnitsPerHour;
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
            if (statValue < MinValue) {
                return MinValue;
            }
            return statValue > s_MaxValue ? s_MaxValue : statValue;
        }

        /// <summary>
        /// Returns the color for a specific rank.
        /// </summary>
        static public Color RankColor(ushort statValue) {
            Assert.NotNull(s_Gradient, "Stats not loaded");
            int amt = (int) ((s_Gradient.Length - 1) * (float) statValue / s_MaxValue);
            return s_Gradient[amt];
        }

        /// <summary>
        /// Retrieves the stat definition for the given stat id.
        /// </summary>
        static public StatsDef.Stat Info(StatId statId) {
            Assert.NotNull(s_Stats, "Stats not loaded");
            return s_Stats[(int) statId];
        }


        /// <summary>
        /// Retrieves the name of the stat with the given stat id.
        /// </summary>
        static public LocId Name(StatId statId) {
            return Info(statId).NameId;
        }

        /// <summary>
        /// Returns the label for the given stat with the given stat value.
        /// </summary>
        static public LocId RankLabel(StatId statId, ushort statValue) {
            var stat = Info(statId);
            int rankIndex = statValue / stat.RankInterval;
            if (rankIndex > stat.RankNameIds.Length - 1) {
                rankIndex = stat.RankNameIds.Length - 1;
            }
            return stat.RankNameIds[rankIndex];
        }
    }
}