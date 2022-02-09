using BeauUtil;
using BeauData;
using BeauUtil.Variants;
using System.Collections.Generic;
using Leaf.Runtime;
using BeauUtil.Debugger;

namespace Journalism {
    static public class Player {
        static private PlayerData s_Current;
        static private CustomVariantResolver s_Resolver;

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
            return s_Current.StatValues[(int) statId] >= value;
        }

        /// <summary>
        /// Returns if the given node has been visited.
        /// </summary>
        [LeafMember("Visited")]
        static public bool Visited(StringHash32 nodeId) {
            return s_Current.VisitedNodeIds.Contains(nodeId);
        }

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
    }
}