using BeauUtil;
using BeauData;
using BeauUtil.Variants;
using System.Collections.Generic;
using Leaf.Runtime;
using BeauUtil.Debugger;
using Leaf;
using System;
using UnityEngine.Scripting;
using Journalism.UI;

namespace Journalism {
    static public class Player {
        static private PlayerData s_Current;
        static private CustomVariantResolver s_Resolver;
        static private StoryStats s_Stats;

        static private readonly StringUtils.ArgsList.Splitter s_ArgsSplitter = new StringUtils.ArgsList.Splitter();

        /// <summary>
        /// Registers data.
        /// </summary>
        static internal void DeclareData(PlayerData data, CustomVariantResolver resolver) {
            s_Current = data;
            s_Resolver = resolver;
        }

        /// <summary>
        /// Returns the current data.
        /// </summary>
        static public PlayerData Data {
            get { return s_Current; }
        }

        #region Location

        /// <summary>
        /// Returns the id of the current location.
        /// </summary>
        [LeafMember("Location"), Preserve]
        static public StringHash32 Location() {
            return s_Current.LocationId;
        }

        /// <summary>
        /// Sets the current location id.
        /// </summary>
        [LeafMember("SetLocation"), Preserve]
        static public bool SetLocation(StringHash32 locationId) {
            if (s_Current.LocationId != locationId) {
                s_Current.LocationId = locationId;
                Game.Events.Dispatch(GameEvents.LocationUpdated, locationId);
                return true;
            }

            return false;
        }

        #endregion // Location

        #region Stats

        /// <summary>
        /// Returns the value for the current save's given stat.
        /// </summary>
        [LeafMember("Stat"), Preserve]
        static public int Stat(StatId statId) {
            return s_Current.StatValues[(int) statId];
        }

        /// <summary>
        /// Returns if the player's stat value is greater than or equal to the given value.
        /// </summary>
        [LeafMember("StatCheck"), Preserve]
        static public bool StatCheck(StatId statId, int value) {
            if (s_Current.StatValues[(int) statId] >= value) {
                Log.Msg("[Player] Stat check: Player Stat {0} ({1}) vs {2} - success!", statId, s_Current.StatValues[(int) statId], value);
                return true;
            } else {
                Log.Msg("[Player] Stat check: Player Stat {0} ({1}) vs {2} - failure :(", statId, s_Current.StatValues[(int) statId], value);
                return false;
            }
        }

        /// <summary>
        /// Sets a stat value directly.
        /// </summary>
        [LeafMember("SetStat"), Preserve]
        static public bool SetStat(StatId statId, int value) {
            int clamped = Stats.Clamp(value);
            ref int current = ref s_Current.StatValues[(int) statId];
            if (current != clamped) {
                Log.Msg("[Player] Stat {0} changed from {1} to {2}", statId, current, clamped);
                current = (ushort) clamped;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adjust stats.
        /// </summary>
        [LeafMember("AdjustStats"), Preserve]
        static public void AdjustStats(StringSlice statData) {
            AdjustStatsImpl(statData, true);
        }

        /// <summary>
        /// Initializes stats.
        /// </summary>
        [LeafMember("InitStats"), Preserve]
        static public void InitStats(StringSlice statData) {
            AdjustStatsImpl(statData, false);
        }

        static private void AdjustStatsImpl(StringSlice statData, bool dispatchEvent) {
            if (statData.IsEmpty) {
                return;
            }

            TempList8<StringSlice> allAdjustments = default;
            statData.Split(s_ArgsSplitter, System.StringSplitOptions.RemoveEmptyEntries, ref allAdjustments);
            int[] adjustments = new int[Stats.Count];
            bool bChanged = false;
            
            foreach(var adjustStr in allAdjustments)
            {
                int operatorIdx = adjustStr.IndexOfAny(AdjustOperators);
                Assert.True(operatorIdx >= 0, "Stat modification '{0}' is not valid", adjustStr);
                StringSlice left = adjustStr.Substring(0, operatorIdx);
                StringSlice right = adjustStr.Substring(operatorIdx + 1);
                char op = adjustStr[operatorIdx];

                StatId statId = StringParser.ConvertTo<StatId>(left);
                int value = StringParser.ParseInt(right);

                ref int currentValue = ref s_Current.StatValues[(int) statId];

                int desiredValue = currentValue;
                switch(op) {
                    case '=': {
                        desiredValue = value;
                        break;
                    }
                    case '+': {
                        desiredValue += value;
                        break;
                    }
                    case '-': {
                        desiredValue -= value;
                        break;
                    }
                }

                int finalValue = Stats.Clamp(desiredValue);
                int delta = finalValue - currentValue;
                if (delta != 0) {
                    adjustments[(int) statId] = delta;
                    bChanged = true;

                    Log.Msg("[Player] Stat {0} changed from {1} to {2}", statId, currentValue, finalValue);

                    currentValue = finalValue;
                }
            }

            if (dispatchEvent && bChanged) {
                Game.Events.Dispatch(GameEvents.StatsUpdated, adjustments);
            }
        }
        
        [LeafMember("CityScore"), Preserve]
        static private int LeafGetCityScore() {
            return s_Current.CityScore;
        }

        [LeafMember("AdjustCityScore"), Preserve]
        static private void LeafAdjustCityScore(int change) {
            s_Current.CityScore += change;
            Log.Msg("City score changed by {0}, new value {1}", change, s_Current.CityScore);
        }

        static private readonly char[] AdjustOperators = new char[] {
            '=', '-', '+'
        };

        #endregion // Stats

        #region Time

        /// <summary>
        /// Time update arguments
        /// </summary>
        public struct TimeUpdateArgs {
            public readonly uint Units;
            public readonly int Delta;

            public TimeUpdateArgs(uint current, int delta) {
                Units = current;
                Delta = delta;
            }
        }

        /// <summary>
        /// Returns the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("TimeRemaining"), Preserve]
        static public float TimeRemaining() {
            return Stats.TimeUnitsToHours(s_Current.TimeRemaining);
        }

        /// <summary>
        /// Returns the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("HasTime"), Preserve]
        static public bool HasTime(float hours) {
            return s_Current.TimeRemaining >= Stats.HoursToTimeUnits(hours);
        }

        /// <summary>
        /// Sets the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("SetTimeRemaining"), Preserve]
        static public void SetTimeRemaining(float hours) {
            uint units = Stats.HoursToTimeUnits(hours);
            if (units != s_Current.TimeRemaining) {
                int delta = (int) (units - s_Current.TimeRemaining);
                s_Current.TimeRemaining = units;
                Game.Events.Dispatch(GameEvents.TimeLimitAssigned, new TimeUpdateArgs(s_Current.TimeRemaining, delta));
                Game.Events.Dispatch(GameEvents.TimeUpdated, new TimeUpdateArgs(s_Current.TimeRemaining, delta));
            }
        }

        /// <summary>
        /// Decreases the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("DecreaseTime"), Preserve]
        static public void DecreaseTime(float hours) {
            uint units = Stats.HoursToTimeUnits(hours);
            if (units > s_Current.TimeRemaining) {
                units = s_Current.TimeRemaining;
            }
            if (units > 0) {
                s_Current.TimeRemaining -= units;
                Game.Events.Queue(GameEvents.TimeElapsed, new TimeUpdateArgs(s_Current.TimeRemaining, -(int)units));
                Game.Events.Queue(GameEvents.TimeUpdated, new TimeUpdateArgs(s_Current.TimeRemaining, -(int) units));
                if (s_Current.TimeRemaining == 0) {
                    Game.Events.Queue(GameEvents.TimeExpired);
                }
            }
        }

        /// <summary>
        /// Increases the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("IncreaseTime"), Preserve]
        static public void IncreaseTime(float hours) {
            uint units = Stats.HoursToTimeUnits(hours);
            if (units > 0) {
                s_Current.TimeRemaining += units;
                Game.Events.Dispatch(GameEvents.TimeUpdated, new TimeUpdateArgs(s_Current.TimeRemaining, (int) units));
            }
        }

        #endregion // Time

        #region Variables

        /// <summary>
        /// Reads the value of the variable with the given id.
        /// </summary>
        static public Variant ReadVariable(StringSlice varId, object context = null) {
            if (!TableKeyPair.TryParse(varId, out var key)) {
                Log.Error("[Player] Unable to parse '{0}' as a variable key", varId);
                return Variant.Null;
            }
            s_Resolver.TryResolve(context, key, out Variant result);
            return result;
        }

        /// <summary>
        /// Reads the value of the variable with the given id.
        /// </summary>
        static public Variant ReadVariable(TableKeyPair varId, object context = null) {
            s_Resolver.TryResolve(context, varId, out Variant result);
            return result;
        }

        /// <summary>
        /// Writes a value to the variable with the given id.
        /// </summary>
        static public bool WriteVariable(StringSlice varId, Variant value, object context = null) {
            if (!TableKeyPair.TryParse(varId, out var key)) {
                Log.Error("[Player] Unable to parse '{0}' as a variable key", varId);
                return false;
            }
            s_Resolver.TryGetVariant(context, key, out Variant current);
            if (value != current && s_Resolver.TryModify(context, key, VariantModifyOperator.Set, value)) {
                Game.Events.Queue(GameEvents.VariableUpdated, key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Writes a value to the variable with the given id.
        /// </summary>
        static public bool WriteVariable(TableKeyPair varId, Variant value, object context = null) {
            s_Resolver.TryGetVariant(context, varId, out Variant current);
            if (value != current && s_Resolver.TryModify(context, varId, VariantModifyOperator.Set, value)) {
                Game.Events.Queue(GameEvents.VariableUpdated, varId);
                return true;
            }
            return false;
        }

        #endregion // Variables

        #region Inventory

        /// <summary>
        /// List of all story scraps accumulated for the current level.
        /// </summary>
        static public ListSlice<StringHash32> StoryScraps {
            get { return s_Current.StoryScrapInventory; }
        }

        /// <summary>
        /// List of all allocated story scraps.
        /// </summary>
        static public ListSlice<StringHash32> AllocatedScraps {
            get { return new ListSlice<StringHash32>(s_Current.AllocatedScraps, 0, Assets.CurrentLevel.Story.Slots.Length); }
        }

        /// <summary>
        /// Returns if a story scrap is in the player's inventory.
        /// </summary>
        [LeafMember("HasSnippet"), Preserve]
        static public bool HasStoryScrap(StringHash32 scrapId) {
            return s_Current.StoryScrapInventory.Contains(scrapId);
        }

        /// <summary>
        /// Returns if a story scrap is in the player's story.
        /// </summary>
        [LeafMember("IncludedSnippet"), Preserve]
        static public bool IncludedStoryScrap(StringHash32 scrapId) {
            return ArrayUtils.Contains(s_Current.AllocatedScraps, scrapId);
        }

        /// <summary>
        /// Adds a story scrap to the player's inventory.
        /// </summary>
        [LeafMember("GiveSnippet"), Preserve]
        static public bool AddStoryScrap(StringHash32 scrapId) {
            if (!s_Current.StoryScrapInventory.Contains(scrapId)) {
                s_Current.StoryScrapInventory.Add(scrapId);
                Log.Msg("[Player] Added story scrap '{0}'!", scrapId);
                Game.Events.Dispatch(GameEvents.InventoryUpdated, scrapId);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the current story stats.
        /// </summary>
        static public StoryStats StoryStatistics {
            get { return s_Stats; }
        }

        /// <summary>
        /// Compiles current story statistics.
        /// </summary>
        static public void CompileStoryStatistics() {
            s_Stats = StoryStats.FromPlayerData(s_Current, Assets.CurrentLevel.Story);
            Log.Msg("[Player] Story Statistics: Quality {0} ({1} - {2}) / Alignment {3} / Score {4}", s_Stats.TotalQuality, s_Stats.QualityAdd, s_Stats.QualitySubtract, s_Stats.Alignment, s_Stats.Score);
        }

        /// <summary>
        /// Removes used snippets from the player's inventory.
        /// </summary>
        static public void RemoveUsedSnippets() {
            foreach(var scrap in s_Current.AllocatedScraps) {
                if (!scrap.IsEmpty) {
                    s_Current.StoryScrapInventory.Remove(scrap);
                }
            }
        }

        [LeafMember("StoryScore"), Preserve]
        static private StringHash32 LeafGetStoryScore() {
            switch(s_Stats.Score) {
                case StoryScore.Bad: {
                    return "bad";
                }
                case StoryScore.Medium: {
                    return "medium";
                }
                case StoryScore.Good: {
                    return "good";
                }
                default: {
                    Assert.Fail("Unknown story score");
                    return null;
                }
            }
        }

        [LeafMember("StoryAlignment"), Preserve]
        static private float LeafGetStoryAlignment() {
            return s_Stats.Alignment;
        }

        [LeafMember("StoryNetQuality"), Preserve]
        static private float LeafGetStoryQuality() {
            return s_Stats.TotalQuality;
        }

        [LeafMember("StoryQuality"), Preserve]
        static private float LeafGetStoryQualityCount() {
            return s_Stats.QualityAdd;
        }

        [LeafMember("StoryFluff"), Preserve]
        static private float LeafGetStoryFluffCount() {
            return s_Stats.QualitySubtract;
        }

        [LeafMember("StoryHasPicture"), Preserve]
        static private bool LeafStoryHasPicture() {
            return s_Stats.StoryHasPicture;
        }

        [LeafMember("StoryIsFull"), Preserve]
        static private bool LeafStoryIsFull() {
            return s_Stats.StoryIsFull;
        }

        #endregion // Inventory

        #region Script

        /// <summary>
        /// Returns if the given node has been visited.
        /// </summary>
        [LeafMember("Visited"), Preserve]
        static public bool Visited(StringHash32 nodeId) {
            return s_Current.VisitedNodeIds.Contains(nodeId);
        }

        /// <summary>
        /// Sets up the current level.
        /// </summary>
        static public void SetupLevel(LevelDef def) {
            bool wasActive = s_Current.LevelIndex >= 0;

            if (def.StoryScraps.name != s_Current.StoryGroup) {
                s_Current.StoryGroup = def.StoryScraps.name;
                s_Current.StoryScrapInventory.Clear();
                Array.Clear(s_Current.AllocatedScraps, 0, s_Current.AllocatedScraps.Length);
                Log.Msg("[Player] New story scrap group '{0}' - clearing story inventory", def.StoryScraps.name);
            }

            if (def.LevelIndex != s_Current.LevelIndex) {
                s_Current.StoryScrapInventory.Clear();
                Array.Clear(s_Current.AllocatedScraps, 0, s_Current.AllocatedScraps.Length);
                s_Current.LevelIndex = def.LevelIndex;
                s_Current.CheckpointId = default;
                s_Current.LocationId = default;
                s_Current.TimeRemaining = 0;
                UISystem.SetStoryEnabled(false);
                Log.Msg("[Player] New level index {0} - clearing story and checkpoints", def.LevelIndex);
                Game.Save.SaveCheckpoint(wasActive);
            }

            s_Stats = default;
        }

        #endregion // Script
    }
}