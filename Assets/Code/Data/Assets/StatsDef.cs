using System;
using BeauUtil.Debugger;
using UnityEngine;

namespace Journalism {
    [CreateAssetMenu(menuName = "Journalism Content/Stat Definitions")]
    public class StatsDef : ScriptableObject {
        [Serializable]
        public class Stat {
            public StatId Id;
            
            [Header("Text")] // TODO: Localize these!!
            public string Name;
            public string[] RankNames;

            [Header("Image")]
            public Sprite Icon;

            [NonSerialized] public ushort RankInterval;
        }

        [SerializeField] private Stat[] m_Stats = null;
        [SerializeField] private ushort m_MaxValue = 30;
        [SerializeField] private Gradient m_StatColorGradient = null;
        
        [NonSerialized] private bool m_Processed;

        public ushort MaxValue {
            get { return m_MaxValue; }
        }

        public Stat[] Stats {
            get {
                if (!m_Processed) {
                    Array.Sort(m_Stats, (a, b) => a.Id.CompareTo(b.Id));
                    foreach(var stat in m_Stats) {
                        stat.RankInterval = (ushort) ((m_MaxValue + 1) / stat.RankNames.Length);
                    }
                    m_Processed = true;
                }
                return m_Stats;
            }
        }


        public Gradient StatGradient {
            get { return m_StatColorGradient; }
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
        public const int Count = 6;

        static private ushort s_MaxValue;
        static private StatsDef.Stat[] s_Stats;
        static private Gradient s_Gradient;

        static internal void Import(StatsDef def) {
            s_MaxValue = def.MaxValue;
            s_Stats = def.Stats;
            s_Gradient = def.StatGradient;
        }

        /// <summary>
        /// Clamps the value of the given stat value.
        /// </summary>
        static public uint Clamp(ushort statValue) {
            return statValue > s_MaxValue ? s_MaxValue : statValue;
        }

        /// <summary>
        /// Returns the color for a specific rank.
        /// </summary>
        static public Color RankColor(ushort statValue) {
            Assert.NotNull(s_Gradient, "Stats not loaded");
            return s_Gradient.Evaluate((float) statValue / s_MaxValue);
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
    }
}