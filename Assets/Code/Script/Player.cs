using BeauUtil;
using BeauData;
using BeauUtil.Variants;
using System.Collections.Generic;
using Leaf.Runtime;
using BeauUtil.Debugger;
using Leaf;
using System;

namespace Journalism {
    static public class Player {
        static private PlayerData s_Current;
        static private CustomVariantResolver s_Resolver;

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
        [LeafMember("Location")]
        static public StringHash32 Location() {
            return s_Current.LocationId;
        }

        /// <summary>
        /// Sets the current location id.
        /// </summary>
        [LeafMember("SetLocation")]
        static public bool SetLocation(StringHash32 locationId) {
            if (s_Current.LocationId != locationId) {
                s_Current.LocationId = locationId;
                Game.Events.Dispatch(Events.Locationupdated, locationId);
                return true;
            }

            return false;
        }

        #endregion // Location

        #region Stats

        /// <summary>
        /// Returns the value for the current save's given stat.
        /// </summary>
        [LeafMember("Stat")]
        static public int Stat(StatId statId) {
            return s_Current.StatValues[(int) statId];
        }

        /// <summary>
        /// Returns if the player's stat value is greater than or equal to the given value.
        /// </summary>
        [LeafMember("StatCheck")]
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
        [LeafMember("SetStat")]
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
        [LeafMember("AdjustStats")]
        static public void AdjustStats(StringSlice statData) {
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

            if (bChanged) {
                Game.Events.Dispatch(Events.StatsUpdated, adjustments);
            }
        }

        static private readonly char[] AdjustOperators = new char[] {
            '=', '-', '+'
        };

        #endregion // Stats

        #region Time

        /// <summary>
        /// Returns the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("TimeRemaining")]
        static public float TimeRemaining() {
            return Stats.TimeUnitsToHours(s_Current.TimeRemaining);
        }

        /// <summary>
        /// Returns the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("HasTime")]
        static public bool HasTime(float hours) {
            return s_Current.TimeRemaining >= Stats.HoursToTimeUnits(hours);
        }

        /// <summary>
        /// Sets the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("SetTimeRemaining")]
        static public void SetTimeRemaining(float hours) {
            uint units = Stats.HoursToTimeUnits(hours);
            if (units != s_Current.TimeRemaining) {
                s_Current.TimeRemaining = units;
                Game.Events.DispatchAsync(Events.TimeUpdated, s_Current.TimeRemaining);
            }
        }

        /// <summary>
        /// Decreases the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("DecreaseTime")]
        static public void DecreaseTime(float hours) {
            uint units = Stats.HoursToTimeUnits(hours);
            if (units > s_Current.TimeRemaining) {
                units = s_Current.TimeRemaining;
            }
            if (units > 0) {
                s_Current.TimeRemaining -= units;
                Game.Events.DispatchAsync(Events.TimeUpdated, s_Current.TimeRemaining);
            }
        }

        /// <summary>
        /// Increases the amount of time remaining, in hours.
        /// </summary>
        [LeafMember("IncreaseTime")]
        static public void IncreaseTime(float hours) {
            uint units = Stats.HoursToTimeUnits(hours);
            if (units > 0) {
                s_Current.TimeRemaining += units;
                Game.Events.DispatchAsync(Events.TimeUpdated, s_Current.TimeRemaining);
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
        static public void WriteVariable(StringSlice varId, Variant value, object context = null) {
            if (!TableKeyPair.TryParse(varId, out var key)) {
                Log.Error("[Player] Unable to parse '{0}' as a variable key", varId);
                return;
            }
            if (s_Resolver.TryModify(context, key, VariantModifyOperator.Set, value)) {
                Game.Events.DispatchAsync(Events.VariableUpdated, key);
            }
        }

        /// <summary>
        /// Writes a value to the variable with the given id.
        /// </summary>
        static public void WriteVariable(TableKeyPair varId, Variant value, object context = null) {
            if (s_Resolver.TryModify(context, varId, VariantModifyOperator.Set, value)) {
                Game.Events.DispatchAsync(Events.VariableUpdated, varId);
            }
        }

        #endregion // Variables

        #region Inventory

        /// <summary>
        /// List of all story scraps accumulated for the current level.
        /// </summary>
        /// <value></value>
        static public ListSlice<StringHash32> StoryScraps {
            get { return s_Current.StoryScrapInventory; }
        }

        /// <summary>
        /// Returns if a story scrap is in the player's inventory.
        /// </summary>
        [LeafMember("HasSnippet")]
        static public bool HasStoryScrap(StringHash32 scrapId) {
            return s_Current.StoryScrapInventory.Contains(scrapId);
        }

        /// <summary>
        /// Adds a story scrap to the player's inventory.
        /// </summary>
        [LeafMember("GiveSnippet")]
        static public bool AddStoryScrap(StringHash32 scrapId) {
            if (!s_Current.StoryScrapInventory.Contains(scrapId)) {
                s_Current.StoryScrapInventory.Add(scrapId);
                Game.Events.Dispatch(Events.InventoryUpdated, scrapId);
                return true;
            }

            return false;
        }

        #endregion // Inventory

        #region Script

        /// <summary>
        /// Returns if the given node has been visited.
        /// </summary>
        [LeafMember("Visited")]
        static public bool Visited(StringHash32 nodeId) {
            return s_Current.VisitedNodeIds.Contains(nodeId);
        }

        /// <summary>
        /// Resets level-specific data.
        /// </summary>
        [LeafMember("ResetForNewLevel")]
        static public void ResetForNewLevel() {
            s_Current.StoryScrapInventory.Clear();
            Array.Clear(s_Current.AllocatedScraps, 0, s_Current.AllocatedScraps.Length);
            s_Current.CheckpointId = default;
            s_Current.LocationId = default;
        }

        #endregion // Script
    }
}